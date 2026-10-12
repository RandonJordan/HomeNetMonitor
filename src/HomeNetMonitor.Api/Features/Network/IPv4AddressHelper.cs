using System.Net;      

namespace HomeNetMonitor.Api.Features.Network;

public static class IPv4AddressHelper
{
    public static uint ToUInt32(string ipAddress)
    {
        var bytes = IPAddress.Parse(ipAddress).GetAddressBytes();

        return ((uint)bytes[0] << 24)
            | ((uint)bytes[1] << 16)
            | ((uint)bytes[2] << 8)
            | bytes[3];
    }
}