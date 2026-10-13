namespace HomeNetMonitor.Api.Data.Entities;

public class NetworkDevice
{
    public int Id { get; set; }

    public string? IpAddress { get; set; }

    public string? MacAddress { get; set; }

    public string? Hostname { get; set; }

    public string? DisplayName { get; set; }

    public string? Model { get; set; }

    public string? Vendor { get; set; }

    public bool IsLocallyAdministered { get; set; }

    public DateTime FirstSeenUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }

    public List<DeviceObservations> Observations { get; set; } = new();
}