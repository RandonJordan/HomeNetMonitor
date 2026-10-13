namespace HomeNetMonitor.Api.Data.Entities;

public class DeviceObservations
{
    public long Id { get; set; }

    //foreign key for NetworkDevice table
    public int NetworkDeviceId { get; set; }

    public NetworkDevice NetworkDevice { get; set; } = null!;

    public DateTime ObservedAtUtc { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public bool RespondedToPing { get; set; }

    public long? RoundtripTimeMs { get; set; }
}