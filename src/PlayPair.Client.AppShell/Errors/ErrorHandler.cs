using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace PlayPair.Client.AppShell.Errors;

public sealed class ErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;

    public event EventHandler<UserFriendlyErrorEventArgs>? UserFriendlyErrorOccurred;

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void HandleError(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        _logger.LogError(exception, "Error occurred");

        string userMessage = exception switch
        {
            ArgumentNullException ane => $"Internal error: {ane.ParamName} is required.",
            ArgumentException ae => $"Invalid input: {ae.Message}",
            InvalidOperationException ioe => "Operation not allowed in current state. Please try again.",
            OperationCanceledException => "Operation was cancelled.",
            TimeoutException => "Operation timed out. Please check your connection.",
            _ => "An unexpected error occurred. Please try again or contact support."
        };

        RaiseUserFriendlyError("UNEXPECTED_ERROR", userMessage);
    }

    public void HandleHubException(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            HandleError(new ArgumentNullException(nameof(payload)));
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            string errorCode = root.GetProperty("code").GetString() ?? "UNKNOWN";
            string errorMessage = root.GetProperty("message").GetString() ?? "Unknown error";
            string connectionId = root.GetProperty("connectionId").GetString() ?? "unknown";

            _logger.LogError("Server error: {ErrorCode} - {ErrorMessage} (ConnectionId: {ConnectionId})", 
                errorCode, errorMessage, connectionId);

            string userMessage = errorCode switch
            {
                "ROOM_NOT_FOUND" => "Room code is invalid or has expired. Please create a new room.",
                "INVALID_ROOM_CODE" => "Room code format is invalid. Please check and try again.",
                "ROOM_FULL" => "This room is already full (max 2 participants). Please create a new room.",
                "NOT_HOST" => "Only the host can perform this action.",
                "COMMAND_OUT_OF_ORDER" => "Command received out of order. Sync recovery in progress.",
                "STALE_COMMAND" => "Command is too old and cannot be applied.",
                "INVALID_PLAYER_STATE" => "Player state is invalid. Please try a different action.",
                "CLIENT_NOT_IN_ROOM" => "You are not in a room. Please create or join one.",
                _ => errorMessage
            };

            RaiseUserFriendlyError(errorCode, userMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse hub exception payload: {Payload}", payload);
            RaiseUserFriendlyError("PARSE_ERROR", "Failed to parse server error. Please reconnect.");
        }
    }

    public void HandleValidationError(string fieldName, string reason)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentNullException(nameof(fieldName));

        string userMessage = fieldName switch
        {
            "roomCode" => "Room code must be 6 alphanumeric characters.",
            "displayName" => "Display name must be between 1 and 50 characters.",
            _ => $"Invalid {fieldName}: {reason}"
        };

        _logger.LogWarning("Validation error: {FieldName} - {Reason}", fieldName, reason);
        RaiseUserFriendlyError("VALIDATION_ERROR", userMessage);
    }

    public void HandleNetworkError(string context)
    {
        string userMessage = string.IsNullOrWhiteSpace(context)
            ? "Network connection failed. Please check your internet connection and try again."
            : $"Network error during {context}. Please check your connection and try again.";

        _logger.LogError("Network error: {Context}", context ?? "unknown");
        RaiseUserFriendlyError("NETWORK_ERROR", userMessage);
    }

    public void HandleConnectionTimeout()
    {
        const string userMessage = "Connection timed out. Please check your internet connection and try reconnecting.";
        _logger.LogError("Connection timeout");
        RaiseUserFriendlyError("TIMEOUT", userMessage);
    }

    private void RaiseUserFriendlyError(string errorCode, string userMessage)
    {
        UserFriendlyErrorOccurred?.Invoke(this, new UserFriendlyErrorEventArgs(errorCode, userMessage));
    }
}

public sealed class UserFriendlyErrorEventArgs : EventArgs
{
    public string ErrorCode { get; }
    public string UserMessage { get; }

    public UserFriendlyErrorEventArgs(string errorCode, string userMessage)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        UserMessage = userMessage ?? throw new ArgumentNullException(nameof(userMessage));
    }
}
