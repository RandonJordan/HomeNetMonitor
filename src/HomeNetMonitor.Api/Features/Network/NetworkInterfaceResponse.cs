namespace HomeNetMonitor.Api.Features.Network;

public record NetworkInterfaceResponse(
    string Id,
    string Name,
    string Description,
    string InterfaceType,
    string OperationalStatus,
    string MacAddress,
    IReadOnlyList<IPv4AddressResponse> IPv4Addresses,
    IReadOnlyList<string> GatewayAddresses,
    IReadOnlyList<string> DnsServers
);

public record IPv4AddressResponse(
    string Address,
    string SubnetMask
);