using HomeNetMonitor.Api.Features.Network;

namespace HomeNetMonitor.Api.Tests;

public class IPv4AddressHelperTest
{
    [Fact]
    public void ToUInt32_ShouldPreserveIpv4NumericOrder()
    {
        var addresses = new[]
        {
            "192.168.69.200",
            "192.168.68.200",
            "192.168.69.10"
        };

        var sortedAddresses = addresses
            .OrderBy(IPv4AddressHelper.ToUInt32)
            .ToList();

        Assert.Equal(
            new[]
            {
            "192.168.68.200",
            "192.168.69.10",
            "192.168.69.200"
            },
            sortedAddresses
        );
    }
}