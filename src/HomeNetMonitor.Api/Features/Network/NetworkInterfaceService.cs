using System.Net.Sockets;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Authorization.Policy;

namespace HomeNetMonitor.Api.Features.Network;

public class NetworkInterfaceService
{
    public IReadOnlyList<NetworkInterfaceResponse> GetNetworkInterfaces()
    {
        var responses = new List<NetworkInterfaceResponse>();

        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach(var networkInterface in networkInterfaces)
        {
            var ipProperties = networkInterface.GetIPProperties();

            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }


            var ipv4Addresses = ipProperties.UnicastAddresses
                .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(address => new IPv4AddressResponse(Address: address.Address.ToString(),SubnetMask: address.IPv4Mask?.ToString() ?? string.Empty)).ToList();
                
            if (ipv4Addresses.Count == 0)
            {
                continue;
            }

            var gateWayAddresses = ipProperties.GatewayAddresses
                .Where(gateway => gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(gateway => gateway.Address.ToString()).ToList();


            var dnsServers = ipProperties.DnsAddresses
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .Select(address => address.ToString()).ToList();

            var macAddress = FormatMacAddress(
                networkInterface.GetPhysicalAddress());


            var response = new NetworkInterfaceResponse(
                Id: networkInterface.Id,
                Name: networkInterface.Name,
                Description: networkInterface.Description,
                InterfaceType: networkInterface.NetworkInterfaceType.ToString(),
                OperationalStatus: networkInterface.OperationalStatus.ToString(),
                MacAddress: macAddress,
                IPv4Addresses: ipv4Addresses,
                GatewayAddresses: gateWayAddresses,
                DnsServers: dnsServers
            );
            responses.Add(response);
        }

        return responses;
    }

    private static string FormatMacAddress(PhysicalAddress physicalAddress)
    {
        var bytes = physicalAddress.GetAddressBytes();

        return string.Join(":", bytes.Select(value => value.ToString("X2")));
    }
}