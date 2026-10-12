using HomeNetMonitor.Api.Features.Network;

namespace HomeNetMonitor.Api.Tests;

public class SubnetCalculatorTests
{
    
    [Fact]
    public void CalculateShouldReturnCorrectSubnet_For24BitsMask()
    {
        //Arrange
        var calculator = new SubnetCalculator();

        var ipAddress = "192.168.68.109";
        var subnetMask = "255.255.255.0";

        //Act
        var result = calculator.Calculate(ipAddress,subnetMask);

        //Assert
        Assert.Equal("192.168.68.0", result.NetworkAddress);
        Assert.Equal("192.168.68.255", result.BroadcastAddress);
        Assert.Equal("192.168.68.1", result.FirstUsableAddress);
        Assert.Equal("192.168.68.254", result.LastUsableAddress);
        Assert.Equal(24, result.PrefixLength);
        Assert.Equal(254, result.UsableHostCount);

    }

    [Fact]
    public void Calculate_ShouldReturnCorrectSubnet_for23BitMask()
    {
        var calculator = new SubnetCalculator();

        var ipAddress = "192.168.69.109";
        var subnetMask = "255.255.254.0";

        var result = calculator.Calculate(ipAddress, subnetMask);

        // Assert
        Assert.Equal("192.168.68.0", result.NetworkAddress);
        Assert.Equal("192.168.69.255", result.BroadcastAddress);
        Assert.Equal("192.168.68.1", result.FirstUsableAddress);
        Assert.Equal("192.168.69.254", result.LastUsableAddress);
        Assert.Equal(23, result.PrefixLength);
        Assert.Equal(510, result.UsableHostCount);
    }

    [Fact]
    public void Calculate_ShouldReturnCorrectSubnet_For22BitMask()
    {
        // Arrange
        var calculator = new SubnetCalculator();

        var ipAddress = "192.168.71.109";
        var subnetMask = "255.255.252.0";

        // Act
        var result = calculator.Calculate(
            ipAddress,
            subnetMask
        );

        // Assert
        Assert.Equal("192.168.68.0", result.NetworkAddress);
        Assert.Equal("192.168.71.255", result.BroadcastAddress);
        Assert.Equal("192.168.68.1", result.FirstUsableAddress);
        Assert.Equal("192.168.71.254", result.LastUsableAddress);
        Assert.Equal(22, result.PrefixLength);
        Assert.Equal(1022, result.UsableHostCount);
    }
    [Fact]
    public void Calculate_ShouldHandle31BitPointToPointSubnet()
    {
        // Arrange
        var calculator = new SubnetCalculator();

        var ipAddress = "192.168.68.10";
        var subnetMask = "255.255.255.254";

        // Act
        var result = calculator.Calculate(
            ipAddress,
            subnetMask
        );

        // Assert
        Assert.Equal("192.168.68.10", result.NetworkAddress);
        Assert.Equal("192.168.68.11", result.BroadcastAddress);
        Assert.Equal("192.168.68.10", result.FirstUsableAddress);
        Assert.Equal("192.168.68.11", result.LastUsableAddress);
        Assert.Equal(31, result.PrefixLength);
        Assert.Equal(2, result.UsableHostCount);
    }

    [Fact]
    public void Calculate_ShouldHandle32BitHostRoute()
    {
        // Arrange
        var calculator = new SubnetCalculator();

        var ipAddress = "192.168.68.109";
        var subnetMask = "255.255.255.255";

        // Act
        var result = calculator.Calculate(
            ipAddress,
            subnetMask
        );

        // Assert
        Assert.Equal("192.168.68.109", result.NetworkAddress);
        Assert.Equal("192.168.68.109", result.BroadcastAddress);
        Assert.Equal("192.168.68.109", result.FirstUsableAddress);
        Assert.Equal("192.168.68.109", result.LastUsableAddress);
        Assert.Equal(32, result.PrefixLength);
        Assert.Equal(1, result.UsableHostCount);
    }
}