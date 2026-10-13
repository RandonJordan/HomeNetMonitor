using System;
using System.Data;
using HomeNetMonitor.Api.Data;
using HomeNetMonitor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeNetMonitor.Api.Features.Network.Persistence;

public class DevicePersistenceService : IDevicePersistenceService
{
    private readonly HomeNetMonitorDbContext _dbContext;

    public DevicePersistenceService(HomeNetMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveDiscoveredDevicesAsync(
    IReadOnlyList<DiscoveredDeviceResponse> devices,
    CancellationToken cancellationToken = default)
{
    var now = DateTime.UtcNow;

    foreach (var device in devices)
    {
        // Only store devices that responded to ping for now.
        if (!device.IsReachable)
        {
            continue;
        }

        // First, try to identify the device using a reliable MAC.
        NetworkDevice? existingDevice = null;

        if (!string.IsNullOrWhiteSpace(device.MacAddress))
        {
            existingDevice = await _dbContext.NetworkDevices
                .FirstOrDefaultAsync(
                    d => d.MacAddress == device.MacAddress,
                    cancellationToken);
        }

        // If found device, update its information.
        if (existingDevice != null)
        {
            existingDevice.IpAddress = device.IpAddress;
            existingDevice.Hostname = device.HostName;
            existingDevice.DisplayName = device.DisplayName;
            existingDevice.Model = device.Model;
            existingDevice.Vendor = device.Vendor;
            existingDevice.LastSeenUtc = now;
        }
        else
        {
            existingDevice = new NetworkDevice
            {
                IpAddress = device.IpAddress,
                MacAddress = device.MacAddress,
                Hostname = device.HostName,
                DisplayName = device.DisplayName,
                Model = device.Model,
                Vendor = device.Vendor,
                IsLocallyAdministered = device.IsLocallyAdministered,
                FirstSeenUtc = now,
                LastSeenUtc = now
            };

             _dbContext.NetworkDevices.Add(existingDevice);
        }

        var observation = new DeviceObservations
        {
            NetworkDevice = existingDevice,
            ObservedAtUtc = now,
            IpAddress = device.IpAddress,
            RespondedToPing = device.IsReachable,
            RoundtripTimeMs = device.RoundtripTimeMs
        };

        _dbContext.DeviceObservation.Add(observation);

    }

    await _dbContext.SaveChangesAsync(cancellationToken);
}
}