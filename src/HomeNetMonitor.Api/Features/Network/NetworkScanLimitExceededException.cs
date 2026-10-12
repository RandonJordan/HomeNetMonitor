namespace HomeNetMonitor.Api.Features.Network;

public class NetworkScanLimitExceededException : Exception
{
    public NetworkScanLimitExceededException(string message) : base(message)
    {
    }
}