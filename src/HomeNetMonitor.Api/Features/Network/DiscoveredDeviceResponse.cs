namespace HomeNetMonitor.Api.Features.Network;

public record DiscoveredDeviceResponse(
    string IpAddress,
    string? HostName,
    string? DisplayName,
    string? Model,
    string? MacAddress,
    string? Vendor,
    bool IsLocallyAdministered,
    bool IsReachable,
    long? RoundtripTimeMs,
    IReadOnlyList<DiscoveredServiceResponse> Services
);