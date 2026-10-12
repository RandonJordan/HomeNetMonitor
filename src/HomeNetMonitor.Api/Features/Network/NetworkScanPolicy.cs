namespace HomeNetMonitor.Api.Features.Network;

public static class NetworkScanPolicy
{
    public const int MaxHostsPerScan = 1024;

    public static void Validate(SubnetInfo subnet)
    {
        if (subnet.UsableHostCount > MaxHostsPerScan)
        {
            throw new NetworkScanLimitExceededException(
                $"Subnet contains {subnet.UsableHostCount} usable hosts. " +
                $"The maximum allowed scan size is {MaxHostsPerScan}."
            );
        }
    }
}