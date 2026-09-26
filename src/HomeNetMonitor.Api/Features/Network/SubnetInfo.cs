

namespace HomeNetMonitor.Api.Features.Network;

public record SubnetInfo(
    string NetworkAddress, 
    string BroadcastAddress, 
    string FirstUsableAddress, 
    string LastUsableAddress, 
    int PrefixLength, 
    int UsableHostCount
);
