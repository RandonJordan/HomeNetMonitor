namespace HomeNetMonitor.Api.Features.Network;

public record ArpEntryResponse(
    string IpAddress,
    string MacAddress,
    string InterfaceName
);