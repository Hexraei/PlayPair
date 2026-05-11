using Microsoft.Extensions.Logging;
using Xunit;
using PlayPair.Client.AppShell.Errors;
using Microsoft.Extensions.Logging.Abstractions;

namespace PlayPair.Client.AppShell.Tests.Connection;

public class ErrorHandlerTests
{
    [Fact]
    public void FormatError_WithValidCode_ReturnsUserFriendlyMessage()
    {
        var errorHandler = new ErrorHandler(NullLogger<ErrorHandler>.Instance);
        
        var message = errorHandler.FormatError("ROOM_NOT_FOUND");
        
        Assert.NotEmpty(message);
        Assert.DoesNotContain("error code", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatError_WithUnknownCode_ReturnsFallbackMessage()
    {
        var errorHandler = new ErrorHandler(NullLogger<ErrorHandler>.Instance);
        
        var message = errorHandler.FormatError("UNKNOWN_CODE");
        
        Assert.NotEmpty(message);
        Assert.Contains("operation failed", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseHubException_WithValidJson_ReturnsErrorCode()
    {
        var errorHandler = new ErrorHandler(NullLogger<ErrorHandler>.Instance);
        var jsonPayload = @"{""code"":""INVALID_ROOM_CODE"",""message"":""Room code invalid""}";
        
        var errorCode = errorHandler.ParseHubExceptionPayload(jsonPayload);
        
        Assert.Equal("INVALID_ROOM_CODE", errorCode);
    }

    [Fact]
    public void ParseHubException_WithInvalidJson_ReturnsFallback()
    {
        var errorHandler = new ErrorHandler(NullLogger<ErrorHandler>.Instance);
        
        var errorCode = errorHandler.ParseHubExceptionPayload("not-json");
        
        Assert.NotEmpty(errorCode);
    }
}
