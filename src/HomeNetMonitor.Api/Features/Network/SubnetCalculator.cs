using System.Net;
using System.Threading.Tasks.Dataflow;

namespace HomeNetMonitor.Api.Features.Network;

public class SubnetCalculator()
{
    public SubnetInfo Calculate(string ipAddress, string subnetMask)
    {
        var ipBytes = IPAddress.Parse(ipAddress).GetAddressBytes();
        var maskBytes = IPAddress.Parse(subnetMask).GetAddressBytes();

        var networkBytes = new byte[ipBytes.Length];
        var broadcastBytes = new byte[ipBytes.Length];

        for(var i = 0; i < ipBytes.Length; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);

            broadcastBytes[i] = (byte)(networkBytes[i] | ~maskBytes[i]);
        };

        var prefixLength = GetPrefixLength(maskBytes);

        var networkAddress = new IPAddress(networkBytes);
        var broadcastAddress = new IPAddress(broadcastBytes);

        var networkValue = ToUInt32(networkBytes);
        var broadcastValue = ToUInt32(broadcastBytes);

        var firstUsableValue = networkValue + 1;
        var lastUsableValue = broadcastValue - 1;

        var usableHostCount =(int)(broadcastValue - networkValue - 1);

        return new SubnetInfo(
            NetworkAddress: networkAddress.ToString(),
            BroadcastAddress: broadcastAddress.ToString(),
            FirstUsableAddress: FromUInt32(firstUsableValue),
            LastUsableAddress: FromUInt32(lastUsableValue),
            PrefixLength: prefixLength,
            UsableHostCount: usableHostCount

        );

    }

    private static int GetPrefixLength(byte[] maskBytes)
    {
        var prefixLength = 0;

        foreach(var maskByte in maskBytes)
        {
            prefixLength += CountBits(maskByte);
        }

        return prefixLength;
    }

    private static int CountBits(byte value)
    {
        var count = 0;

        while(value != 0)
        {
            count += value & 1;
            value >>=1;
        }

        return count;
    }

    private static uint ToUInt32(byte[] bytes)
    {
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }


    private static string FromUInt32(uint value)
    {
        var bytes = new[]
        {
            (byte)(value>>24),
            (byte)(value>>16),
            (byte)(value>>8),
            (byte)value
        };

        return new IPAddress(bytes).ToString();
    }
}