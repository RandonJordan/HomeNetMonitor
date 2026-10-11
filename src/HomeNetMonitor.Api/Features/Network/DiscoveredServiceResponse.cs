namespace HomeNetMonitor.Api.Features.Network;

public record DiscoveredServiceResponse(
    string ServiceType,
    string? DisplayName,
    int Port
);