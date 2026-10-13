using HomeNetMonitor.Api.Data;
using HomeNetMonitor.Api.Features.Network;
using HomeNetMonitor.Api.Features.Network.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using HomeNetMonitor.Api.Data.Entities;

namespace HomeNetMonitor.Api.Tests.Persistence;

public class DevicePersistenceServiceTests
{
    [Fact]
    public async Task SaveDiscoveredDevicesAsync_NewDevice_InsertsDevice()
    {
        // Arrange: Create a temporary SQLite database.
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<HomeNetMonitorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext =
            new HomeNetMonitorDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var service = new DevicePersistenceService(dbContext);

        var discoveredDevices = new List<DiscoveredDeviceResponse>
        {
            new(
                IpAddress: "192.168.68.109",
                HostName: "MacBook-Pro",
                DisplayName: "My MacBook",
                Model: null,
                MacAddress: "00:11:22:33:44:55",
                Vendor: "Apple",
                IsLocallyAdministered: false,
                IsReachable: true,
                RoundtripTimeMs: 5,
                Services: []
            )
        };

        // Act: Save the discovered device.
        await service.SaveDiscoveredDevicesAsync(discoveredDevices);

        // Assert: Verify the device was saved.
        var savedDevices = await dbContext.NetworkDevices.ToListAsync();

        Assert.Single(savedDevices);

        var savedDevice = savedDevices[0];

        Assert.Equal("192.168.68.109", savedDevice.IpAddress);
        Assert.Equal("00:11:22:33:44:55", savedDevice.MacAddress);
        Assert.Equal("MacBook-Pro", savedDevice.Hostname);

        Assert.True(savedDevice.FirstSeenUtc > DateTime.MinValue);
        Assert.Equal(savedDevice.FirstSeenUtc, savedDevice.LastSeenUtc);
    }


    [Fact]
    public async Task SaveDiscoveredDevicesAsync_ExistingDevice_UpdatesInsteadOfInserting()
    {
        // Arrange: Create an in-memory SQLite database.
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<HomeNetMonitorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new HomeNetMonitorDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        // Pretend this device was first discovered yesterday.
        var firstSeen = DateTime.UtcNow.AddDays(-1);

        var existingDevice = new NetworkDevice
        {
            IpAddress = "192.168.68.109",
            MacAddress = "00:11:22:33:44:55",
            Hostname = "MacBook-Pro",
            FirstSeenUtc = firstSeen,
            LastSeenUtc = firstSeen
        };

        dbContext.NetworkDevices.Add(existingDevice);
        await dbContext.SaveChangesAsync();

        var service = new DevicePersistenceService(dbContext);

        // The same MacBook is discovered again, but with a new IP.
        var discoveredDevices = new List<DiscoveredDeviceResponse>
        {
            new(
                IpAddress: "192.168.68.125",
                HostName: "MacBook-Pro",
                DisplayName: "My MacBook",
                Model: null,
                MacAddress: "00:11:22:33:44:55",
                Vendor: "Apple",
                IsLocallyAdministered: false,
                IsReachable: true,
                RoundtripTimeMs: 5,
                Services: []
            )
        };

        // Act: Process the new scan.
        await service.SaveDiscoveredDevicesAsync(discoveredDevices);

        // Assert: Verify that the existing row was updated.
        var savedDevices = await dbContext.NetworkDevices.ToListAsync();

        Assert.Single(savedDevices);

        var savedDevice = savedDevices[0];

        Assert.Equal(existingDevice.Id, savedDevice.Id);
        Assert.Equal("192.168.68.125", savedDevice.IpAddress);
        Assert.Equal(firstSeen, savedDevice.FirstSeenUtc);
        Assert.True(savedDevice.LastSeenUtc > firstSeen);
    }

    [Fact]
    public async Task SaveDiscoveredDevicesAsync_LocallyAdministeredMac_DoesNotCreateDuplicate()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<HomeNetMonitorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new HomeNetMonitorDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new DevicePersistenceService(dbContext);

        var discoveredDevices = new List<DiscoveredDeviceResponse>
        {
            new(
                IpAddress: "192.168.68.115",
                HostName: "iPhone",
                DisplayName: "My iPhone",
                Model: null,
                MacAddress: "02:11:22:33:44:55",
                Vendor: null,
                IsLocallyAdministered: true,
                IsReachable: true,
                RoundtripTimeMs: 10,
                Services: []
            )
        };

        // Act: Simulate two scans of the same device.
        await service.SaveDiscoveredDevicesAsync(discoveredDevices);
        await service.SaveDiscoveredDevicesAsync(discoveredDevices);

        // Assert: We should only have one database record.
        var savedDevices = await dbContext.NetworkDevices.ToListAsync();

        Assert.Single(savedDevices);
    }



    [Fact]
    public async Task SaveDiscoveredDevicesAsync_DifferentMacSameIp_KeepsSeparateRecords()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<HomeNetMonitorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new HomeNetMonitorDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new DevicePersistenceService(dbContext);

        var firstDevice = new DiscoveredDeviceResponse(
            IpAddress: "192.168.68.115",
            HostName: "Laptop",
            DisplayName: "Laptop",
            Model: null,
            MacAddress: "00:11:22:33:44:55",
            Vendor: null,
            IsLocallyAdministered: false,
            IsReachable: true,
            RoundtripTimeMs: 5,
            Services: []
        );

        var secondDevice = new DiscoveredDeviceResponse(
            IpAddress: "192.168.68.115",
            HostName: "Phone",
            DisplayName: "Phone",
            Model: null,
            MacAddress: "00:11:22:33:44:66",
            Vendor: null,
            IsLocallyAdministered: false,
            IsReachable: true,
            RoundtripTimeMs: 10,
            Services: []
        );

        // Act: Two different devices use the same IP at different times.
        await service.SaveDiscoveredDevicesAsync([firstDevice]);
        await service.SaveDiscoveredDevicesAsync([secondDevice]);

        // Assert: Both should exist as separate records.
        var savedDevices = await dbContext.NetworkDevices.ToListAsync();

        Assert.Equal(2, savedDevices.Count);
        Assert.NotEqual(savedDevices[0].Id, savedDevices[1].Id);
        Assert.Equal(savedDevices[0].IpAddress, savedDevices[1].IpAddress);
        Assert.NotEqual(savedDevices[0].MacAddress, savedDevices[1].MacAddress);
    }


    [Fact]
    public async Task SaveDiscoveredDevicesAsync_SameDeviceTwice_CreatesTwoObservations()
    {
        // ARRANGE: Create an in-memory SQLite database.
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<HomeNetMonitorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new HomeNetMonitorDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var service = new DevicePersistenceService(dbContext);

        // First scan discovers our MacBook.
        var firstScan = new DiscoveredDeviceResponse(
            IpAddress: "192.168.68.109",
            HostName: "MacBook-Pro",
            DisplayName: "My MacBook",
            Model: null,
            MacAddress: "00:11:22:33:44:55",
            Vendor: "Apple",
            IsLocallyAdministered: false,
            IsReachable: true,
            RoundtripTimeMs: 5,
            Services: []
        );

        // Second scan discovers the same MacBook again.
        // This time, its ping latency is different.
        var secondScan = new DiscoveredDeviceResponse(
            IpAddress: "192.168.68.109",
            HostName: "MacBook-Pro",
            DisplayName: "My MacBook",
            Model: null,
            MacAddress: "00:11:22:33:44:55",
            Vendor: "Apple",
            IsLocallyAdministered: false,
            IsReachable: true,
            RoundtripTimeMs: 12,
            Services: []
        );

        // ACT: Save both scans.
        await service.SaveDiscoveredDevicesAsync([firstScan]);
        await service.SaveDiscoveredDevicesAsync([secondScan]);

        // ASSERT: Read the saved data using a fresh DbContext.
        // This verifies the data was persisted in SQLite,
        // rather than only existing in EF Core's change tracker.
        await using var verificationContext =
            new HomeNetMonitorDbContext(options);

        var savedDevices = await verificationContext.NetworkDevices
            .AsNoTracking()
            .ToListAsync();

        var observations = await verificationContext.DeviceObservation
            .AsNoTracking()
            .OrderBy(o => o.Id)
            .ToListAsync();

        // There should only be ONE device record.
        Assert.Single(savedDevices);

        // But there should be TWO historical observations.
        Assert.Equal(2, observations.Count);

        var savedDevice = savedDevices[0];

        // Both observations should belong to our MacBook.
        Assert.All(observations, observation =>
        {
            Assert.Equal(savedDevice.Id, observation.NetworkDeviceId);
            Assert.Equal("192.168.68.109", observation.IpAddress);
            Assert.True(observation.RespondedToPing);
        });

        // Confirm each scan's ping latency was preserved.
        Assert.Equal(5L, observations[0].RoundtripTimeMs);
        Assert.Equal(12L, observations[1].RoundtripTimeMs);

        // Confirm observation timestamps were recorded.
        Assert.All(observations, observation =>
        {
            Assert.True(observation.ObservedAtUtc > DateTime.MinValue);
        });

        // Confirm timestamps are chronologically consistent.
        Assert.True(
            observations[1].ObservedAtUtc >= observations[0].ObservedAtUtc
        );
    }
}