using System.Text.RegularExpressions;
using Zeroconf;

namespace HomeNetMonitor.Api.Features.Network;

public class BonjourDiscoveryService
{
    public async Task<IReadOnlyList<BonjourDeviceResponse>> DiscoverDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var serviceTypeLookup = await ZeroconfResolver.BrowseDomainsAsync(cancellationToken: cancellationToken);

        var serviceTypes = serviceTypeLookup
            .Select(group => group.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (serviceTypes.Count == 0)
        {
            return [];
        }
        var results = await ZeroconfResolver.ResolveAsync(serviceTypes, cancellationToken: cancellationToken);

        var devices = new List<BonjourDeviceResponse>();

        foreach (var host in results)
        {
            foreach ( var service in host.Services.Values)
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