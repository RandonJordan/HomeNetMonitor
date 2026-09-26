namespace HomeNetMonitor.Api.Features.Network;

public record BonjourDeviceResponse(
    string IpAddress,
    string DisplayName,
    string ServiceType,
    int Port,
    string? Model
);