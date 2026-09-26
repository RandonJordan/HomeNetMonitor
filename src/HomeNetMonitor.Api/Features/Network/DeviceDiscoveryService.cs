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

    public DeviceDiscoveryService(ArpTableService arpTableService)
    {
        _arpTableService = arpTableService;
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

            if (devices.TryGetValue(
                arpEntry.IpAddress,
                out var existingDevice))
            {
                devices[arpEntry.IpAddress] = existingDevice with
                {
                    MacAddress = arpEntry.MacAddress
                };
            }
            else
            {
                devices[arpEntry.IpAddress] =
                    new DiscoveredDeviceResponse(
                        IpAddress: arpEntry.IpAddress,
                        MacAddress: arpEntry.MacAddress,
                        IsReachable: false,
                        RoundtripTimeMs: null
                    );
            }
        }

        return devices.Values
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
                MacAddress: null,
                IsReachable: true,
                RoundtripTimeMs: reply.RoundtripTime
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
}