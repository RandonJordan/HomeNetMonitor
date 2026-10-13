using HomeNetMonitor.Api.Features.Network;
using Microsoft.AspNetCore.Mvc;
using HomeNetMonitor.Api.Features.Network.Persistence;
using HomeNetMonitor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeNetMonitor.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class NetworkController : ControllerBase
{
    private readonly HomeNetMonitorDbContext _dbContext;
    private readonly NetworkInterfaceService _networkInterfaceService;
    private readonly SubnetCalculator _subnetCalculator;
    private readonly DeviceDiscoveryService _deviceDiscoveryService;
    private readonly ArpTableService _arpTableService;
    private readonly BonjourDiscoveryService _bonjourDiscoveryService;
    private readonly MacVendorLookupService _macVendorLookupService;
    private readonly IDevicePersistenceService _devicePersistenceService;

    //injecting DI service below in constructor
    public NetworkController(NetworkInterfaceService networkInterfaceService, SubnetCalculator subnetCalculator, DeviceDiscoveryService deviceDiscoveryService,
    ArpTableService arpTableService, BonjourDiscoveryService bonjourDiscoveryService, MacVendorLookupService macVendorLookupService, IDevicePersistenceService devicePersistenceService
    ,HomeNetMonitorDbContext dbContext)
    {
        _networkInterfaceService = networkInterfaceService;
        _subnetCalculator = subnetCalculator;
        _deviceDiscoveryService = deviceDiscoveryService;
        _arpTableService = arpTableService;
        _bonjourDiscoveryService = bonjourDiscoveryService;
        _macVendorLookupService = macVendorLookupService;
        _devicePersistenceService = devicePersistenceService;
        _dbContext = dbContext;
    }

    [HttpGet("interfaces")]
    public ActionResult<IReadOnlyList<NetworkInterfaceResponse>> GetInterfaces()
    {
        var interfaces = _networkInterfaceService.GetNetworkInterfaces();

        return Ok(interfaces);
    }

    [HttpGet("subnet")]
    public ActionResult<SubnetInfo> GetSubnet()
    {

        var networkInterface = _networkInterfaceService
            .GetNetworkInterfaces()
            .FirstOrDefault(networkInterface => 
            networkInterface.IPv4Addresses.Count > 0 && 
            networkInterface.GatewayAddresses.Count > 0);

        if(networkInterface is null)
        {
            return NotFound("No Active IPv4 netowrk interface was found.");
        }

        var ipv4Address = networkInterface.IPv4Addresses[0];

        if(string.IsNullOrWhiteSpace(ipv4Address.SubnetMask))
        {
            return Problem("The active network interface does not have an IPv4 subnet mask.");
        }

        var subnet = _subnetCalculator.Calculate(
            ipv4Address.Address,
            ipv4Address.SubnetMask
        );

        return Ok(subnet);
    }

    [HttpGet("devices")]
    public async Task<ActionResult<IReadOnlyList<DiscoveredDeviceResponse>>> GetDevices(CancellationToken cancellationToken)
    {
        var networkInterface = _networkInterfaceService
            .GetNetworkInterfaces()
            .FirstOrDefault(networkInterface =>
                networkInterface.IPv4Addresses.Count > 0 &&
                networkInterface.GatewayAddresses.Count > 0);

        if (networkInterface is null)
        {
            return NotFound("No active IPv4 network interface was found.");
        }

        var ipv4Address = networkInterface.IPv4Addresses[0];

        if (string.IsNullOrWhiteSpace(ipv4Address.SubnetMask))
        {
            return Problem("The acitve network inteface does not have an IPv4 subnet mask.");
        }

        var subnet = _subnetCalculator.Calculate(
            ipv4Address.Address,
            ipv4Address.SubnetMask
        );

        // Discover devices currently visible on the network.
        var devices = await _deviceDiscoveryService.ScanAsync(
            subnet,
            cancellationToken
        );

        // Store confirmed devices and their observations in SQLite.
        await _devicePersistenceService.SaveDiscoveredDevicesAsync(
            devices,
            cancellationToken
        );

        // Return the live scan results to the frontend.
        return Ok(devices);

    }

    [HttpGet("arp")]    
    public async Task<ActionResult<IReadOnlyList<ArpEntryResponse>>> GetArpEntries( CancellationToken cancellationToken)
    {
        var entries = await _arpTableService.GetEntriesAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("bonjour")]
    public async Task<ActionResult> GetBonjourDevices(CancellationToken cancellationToken)
    {
        var devices = await _bonjourDiscoveryService
            .DiscoverDevicesAsync(cancellationToken);

        return Ok(devices);
    }

    [HttpGet("devices/saved")]
    public async Task<IActionResult> GetSavedDevices(
        CancellationToken cancellationToken)
    {
        var devices = await _dbContext.NetworkDevices
            .AsNoTracking()
            .OrderBy(device => device.Id)
            .Select(device => new
            {
                device.Id,
                device.IpAddress,
                device.MacAddress,
                device.Hostname,
                device.DisplayName,
                device.Model,
                device.Vendor,
                device.FirstSeenUtc,
                device.LastSeenUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(devices);
    }

    [HttpGet("devices/{deviceId:int}/history")]
    public async Task<IActionResult> GetDeviceHistory(
        int deviceId,
        CancellationToken cancellationToken)
    {
        // Make sure the device exists.
        var deviceExists = await _dbContext.NetworkDevices
            .AsNoTracking()
            .AnyAsync(d => d.Id == deviceId, cancellationToken);

        if (!deviceExists)
        {
            return NotFound($"Device with ID {deviceId} was not found.");
        }

        // Retrieve its historical observations.
        var observations = await _dbContext.DeviceObservation
            .AsNoTracking()
            .Where(o => o.NetworkDeviceId == deviceId)
            .OrderByDescending(o => o.ObservedAtUtc)
            .Select(o => new
            {
                o.Id,
                o.ObservedAtUtc,
                o.IpAddress,
                o.RespondedToPing,
                o.RoundtripTimeMs
            })
            .ToListAsync(cancellationToken);

        return Ok(observations);
    }

}