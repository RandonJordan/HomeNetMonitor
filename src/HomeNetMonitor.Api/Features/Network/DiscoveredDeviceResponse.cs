namespace HomeNetMonitor.Api.Features.Network;

public record DiscoveredDeviceResponse(
    string IpAddress,
    string? HostName,
    string? DisplayName,
    string? Model,
    string? MacAddress,
    bool IsReachable,
    long? RoundtripTimeMs
);