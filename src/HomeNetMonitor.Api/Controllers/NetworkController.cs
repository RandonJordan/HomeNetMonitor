using HomeNetMonitor.Api.Features.Network;
using Microsoft.AspNetCore.Mvc;

namespace HomeNetMonitor.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class NetworkController : ControllerBase
{
    private readonly NetworkInterfaceService _networkInterfaceService;
    private readonly SubnetCalculator _subnetCalculator;
    private readonly DeviceDiscoveryService _deviceDiscoveryService;
    private readonly ArpTableService _arpTableService;
    //injecting DI service below in constructor
    public NetworkController(NetworkInterfaceService networkInterfaceService, SubnetCalculator subnetCalculator, DeviceDiscoveryService deviceDiscoveryService, ArpTableService arpTableService)
    {
        _networkInterfaceService = networkInterfaceService;
        _subnetCalculator = subnetCalculator;
        _deviceDiscoveryService = deviceDiscoveryService;
        _arpTableService = arpTableService;
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

        var devices = await _deviceDiscoveryService.ScanAsync(
            subnet,
            cancellationToken
        );

        return Ok(devices);
    }

    [HttpGet("arp")]
    public async Task<ActionResult<IReadOnlyList<ArpEntryResponse>>> GetArpEntries( CancellationToken cancellationToken)
    {
        var entries = await _arpTableService.GetEntriesAsync(cancellationToken);

        return Ok(entries);
    }
}