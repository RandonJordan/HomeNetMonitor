using HomeNetMonitor.Api.Features.Network;
using HomeNetMonitor.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace HomeNetMonitor.Api.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ShouldReturn400_ForNetworkScanLimitExceededException()
    {
        // Arrange
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();

        httpContext.Response.Body = new MemoryStream();

        var exception = new NetworkScanLimitExceededException(
            "Subnet contains 2046 usable hosts. " +
            "The maximum allowed scan size is 1024."
        );

        // Act
        var handled = await handler.TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None
        );

        // Assert
        Assert.True(handled);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            httpContext.Response.StatusCode
        );
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnFalse_ForUnknownException()
    {
        // Arrange
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();

        var exception = new Exception(
            "Something unexpected happened."
        );

        // Act
        var handled = await handler.TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None
        );

        // Assert
        Assert.False(handled);
    }

}