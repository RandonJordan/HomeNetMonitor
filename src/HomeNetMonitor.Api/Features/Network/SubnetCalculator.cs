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

        uint firstUsable;
        uint lastUsable;
        int usableHostCount;

        if (prefixLength == 32)
        {
            firstUsable = networkValue;
            lastUsable = networkValue;
            usableHostCount = 1;
        }
        else if (prefixLength == 31)
        {
            firstUsable = networkValue;
            lastUsable = broadcastValue;
            usableHostCount = 2;
        }
        else
        {
            firstUsable = networkValue + 1;
            lastUsable = broadcastValue - 1;
            usableHostCount = (int)(broadcastValue - networkValue - 1);
        }


        return new SubnetInfo(
            NetworkAddress: new IPAddress(networkBytes).ToString(),
            BroadcastAddress: new IPAddress(broadcastBytes).ToString(),
            FirstUsableAddress: FromUInt32(firstUsable).ToString(),
            LastUsableAddress: FromUInt32(lastUsable).ToString(),
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