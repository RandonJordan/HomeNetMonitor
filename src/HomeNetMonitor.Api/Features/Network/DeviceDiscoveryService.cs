using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace HomeNetMonitor.Api.Features.Network;

public class DeviceDiscoveryService
{
    private const int PingTimeoutMs = 1500;
    private const int MaxConcurrentPings = 32;
    private readonly ArpTableService _arpTableService;
    private readonly HostnameResolverService _hostnameResolverService;
    private readonly BonjourDiscoveryService _bonjourDiscoveryService;
    private readonly MacVendorLookupService _macVendorLookupService;

    public DeviceDiscoveryService(ArpTableService arpTableService, HostnameResolverService hostnameResolverService, BonjourDiscoveryService bonjourDiscoveryService, MacVendorLookupService macVendorLookupService)
    {
        _arpTableService = arpTableService;
        _hostnameResolverService = hostnameResolverService;
        _bonjourDiscoveryService = bonjourDiscoveryService;
        _macVendorLookupService = macVendorLookupService;
    }

    public async Task<IReadOnlyList<DiscoveredDeviceResponse>> ScanAsync(SubnetInfo subnet, CancellationToken cancellationToken = default)
    {
        var ipAddresses = GetIpAddresses(
            subnet.FirstUsableAddress,
            subnet.LastUsableAddress
        );

        using var semaphore = new SemaphoreSlim(MaxConcurrentPings);

        var pingTasks = ipAddresses.Select(ipAddress =>
            PingAddressAsync(ipAddress, semaphore, cancellationToken)
        );

        var pingResults = await Task.WhenAll(pingTasks);

        var arpEntries = await _arpTableService.GetEntriesAsync(
            cancellationToken
        );

        var devices = pingResults
            .OfType<DiscoveredDeviceResponse>()
            .ToDictionary(
                device => device.IpAddress,
                device => device
            );

        
        var validSubnetAddresses = ipAddresses.ToHashSet();

        foreach (var arpEntry in arpEntries)
        {
            if (!validSubnetAddresses.Contains(arpEntry.IpAddress))
            {
                continue;
            }

            var isLocallyAdministered =
                IsLocallyAdministeredMac(arpEntry.MacAddress);

            var vendor = _macVendorLookupService.GetVendor(
                arpEntry.MacAddress,
                isLocallyAdministered
            );

            if (devices.TryGetValue(
                arpEntry.IpAddress,
                out var existingDevice))
            {
                devices[arpEntry.IpAddress] = existingDevice with
                {
                    MacAddress = arpEntry.MacAddress,
                    Vendor = vendor,
                    IsLocallyAdministered = isLocallyAdministered
                };
            }
            else
            {
                devices[arpEntry.IpAddress] = new DiscoveredDeviceResponse(
                    IpAddress: arpEntry.IpAddress,
                    MacAddress: arpEntry.MacAddress,
                    IsLocallyAdministered: IsLocallyAdministeredMac(arpEntry.MacAddress),
                    DisplayName: null,
                    Model: null,
                    Vendor: vendor,
                    HostName: null,
                    IsReachable: false,
                    RoundtripTimeMs: null,
                    Services: []
                );
            }
        }
        var bonjourDevices =
            await _bonjourDiscoveryService.DiscoverDevicesAsync(
                cancellationToken
            );

        foreach (var bonjourDevice in bonjourDevices)
{
            if (!devices.TryGetValue(
                    bonjourDevice.IpAddress,
                    out var existingDevice))
            {
                continue;
            }

            var services = existingDevice.Services.ToList();

            var serviceAlreadyExists = services.Any(service =>
                service.ServiceType.Equals(
                    bonjourDevice.ServiceType,
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                service.Port == bonjourDevice.Port
            );

            if (!serviceAlreadyExists)
            {
                services.Add(new DiscoveredServiceResponse(
                    ServiceType: bonjourDevice.ServiceType,
                    DisplayName: bonjourDevice.DisplayName,
                    Port: bonjourDevice.Port
                ));
            }

            // Decide whether Bonjour gave us a useful top-level device name
            var displayName = existingDevice.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName)
                && IsUsefulDisplayName(bonjourDevice.DisplayName))
            {
                displayName = bonjourDevice.DisplayName;
            }

            // Put the enriched device back into the dictionary
            devices[bonjourDevice.IpAddress] = existingDevice with
            {
                DisplayName = displayName,

                Model =
                    string.IsNullOrWhiteSpace(existingDevice.Model)
                        ? bonjourDevice.Model
                        : existingDevice.Model,

                Services = services
        };
}

        var hostnameTasks = devices.Values.Select(async device =>
        {
            var hostName = await _hostnameResolverService.ResolveAsync(
                device.IpAddress,
                cancellationToken
                );

            return device with
            {
                HostName = hostName
            };
        });

        var enrichedDevices = await Task.WhenAll(hostnameTasks);

        return enrichedDevices
            .OrderBy(device => ParseLastOctet(device.IpAddress))
            .ToList();

    }

    private static async Task<DiscoveredDeviceResponse?> PingAddressAsync(string ipAddress, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            using var ping = new Ping();

            var reply = await ping
                .SendPingAsync(ipAddress, PingTimeoutMs)
                .WaitAsync(cancellationToken);

            

            if (reply.Status != IPStatus.Success)
            {
                return null;
            }

            return new DiscoveredDeviceResponse(
                IpAddress: ipAddress,
                HostName: null,
                DisplayName: null,
                Model: null,
                IsLocallyAdministered: false,
                MacAddress: null,
                Vendor : null,
                IsReachable: true,
                RoundtripTimeMs: reply.RoundtripTime,
                Services: []
            );
        }
        catch (PingException)
        {
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static IReadOnlyList<string> GetIpAddresses(string firstAddress, string  lastAddress)
    {
        var firstBytes = IPAddress
            .Parse(firstAddress)
            .GetAddressBytes();

        var lastBytes = IPAddress
            .Parse(lastAddress)
            .GetAddressBytes();

        var currentBytes = (byte[])firstBytes.Clone();

        var addresses = new List<string>();

        while (true)
        {
            addresses.Add(new IPAddress(currentBytes).ToString());

            if(currentBytes.SequenceEqual(lastBytes))
            {
                break;
            }

            IncrementAddress(currentBytes);
        }

        return addresses;
    }

    private static void IncrementAddress(byte[] addressBytes)
    {
        for (var i = addressBytes.Length - 1; i >= 0; i--)
        {
            if (addressBytes[i] < 255)
            {
                addressBytes[i]++;
                return;
            }
            addressBytes[i] = 0;
        }
    }

    private static int ParseLastOctet(string ipAddress)
    {
        return int.Parse(
            ipAddress.Split('.')[3]
        );
    }

    private static bool IsUsefulDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return false;

        if (displayName.Contains("._", StringComparison.OrdinalIgnoreCase))
            return false;

        if (displayName.EndsWith(".local.", StringComparison.OrdinalIgnoreCase))
            return false;

        if (displayName.Contains(':'))
            return false;

        if (Guid.TryParse(displayName, out _))
            return false;
        if (displayName.Length >= 12 && displayName.All(Uri.IsHexDigit))
        {
            return false;
        }

        return true;
    }
    private static bool IsLocallyAdministeredMac(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        var firstOctetText = macAddress.Split(':')[0];

        if (!byte.TryParse(
                firstOctetText,
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var firstOctet))
        {
            return false;
        }

        return (firstOctet & 0b00000010) != 0;
    }
}