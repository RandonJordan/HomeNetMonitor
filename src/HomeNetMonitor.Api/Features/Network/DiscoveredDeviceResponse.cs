namespace HomeNetMonitor.Api.Features.Network;

public record DiscoveredDeviceResponse(
    string IpAddress,
    string? MacAddress,
    bool IsReachable,
    long? RoundtripTimeMs
);