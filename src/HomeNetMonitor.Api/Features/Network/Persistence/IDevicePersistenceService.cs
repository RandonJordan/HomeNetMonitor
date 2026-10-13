namespace HomeNetMonitor.Api.Features.Network.Persistence;

public interface IDevicePersistenceService
{
    Task SaveDiscoveredDevicesAsync(
        IReadOnlyList<DiscoveredDeviceResponse> devices,
        CancellationToken cancellationToken = default);
}