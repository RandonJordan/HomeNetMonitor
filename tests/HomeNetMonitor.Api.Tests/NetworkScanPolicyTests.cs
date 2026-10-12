using HomeNetMonitor.Api.Features.Network;

namespace HomeNetMonitor.Api.Tests;

public class NetworkScanPolicyTests
{
    [Fact]
    public void Validate_ShouldAllow22BitSubnet()
    {
        // Arrange
        var subnet = new SubnetInfo(
            NetworkAddress: "192.168.68.0",
            BroadcastAddress: "192.168.71.255",
            FirstUsableAddress: "192.168.68.1",
            LastUsableAddress: "192.168.71.254",
            PrefixLength: 22,
            UsableHostCount: 1022
        );

        // Act
        var exception = Record.Exception(() =>
            NetworkScanPolicy.Validate(subnet)
        );

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_ShouldReject21BitSubnet()
    {
        // Arrange
        var subnet = new SubnetInfo(
            NetworkAddress: "192.168.64.0",
            BroadcastAddress: "192.168.71.255",
            FirstUsableAddress: "192.168.64.1",
            LastUsableAddress: "192.168.71.254",
            PrefixLength: 21,
            UsableHostCount: 2046
        );

        // Act + Assert
        var exception = Assert.Throws<NetworkScanLimitExceededException>(() =>
            NetworkScanPolicy.Validate(subnet)
        );

        Assert.Contains(
            "2046 usable hosts",
            exception.Message
        );
    }
}