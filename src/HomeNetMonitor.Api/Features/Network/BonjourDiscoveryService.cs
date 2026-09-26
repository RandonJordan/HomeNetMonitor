using Zeroconf;

namespace HomeNetMonitor.Api.Features.Network;

public class BonjourDiscoveryService
{
    public async Task<IReadOnlyList<BonjourDeviceResponse>> DiscoverAirPlayDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await ZeroconfResolver.ResolveAsync(
            "_airplay._tcp.local.",
            cancellationToken: cancellationToken
        );

        var devices = new List<BonjourDeviceResponse>();

        foreach (var host in results)
        {
            foreach (var service in host.Services.Values)
            {
                var model = service.Properties
                    .SelectMany(propertySet => propertySet)
                    .FirstOrDefault(property => property.Key.Equals("model", StringComparison.OrdinalIgnoreCase)).Value;

                devices.Add(new BonjourDeviceResponse(
                    IpAddress: host.IPAddress,
                    DisplayName: host.DisplayName,
                    ServiceType: service.Name,
                    Port: service.Port,
                    Model: model

                ));
            }
        }
        return devices;
    }
}