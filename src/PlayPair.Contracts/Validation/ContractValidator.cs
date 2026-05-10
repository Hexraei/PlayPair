using System.Text.Json;
using PlayPair.Contracts.Models;
using PlayPair.Contracts.Protocol;

namespace PlayPair.Contracts.Validation;

public static class ContractValidator
{
    public static ValidationResult ValidateCommand(RoomCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.CommandId))
        {
            errors.Add("commandId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RoomId))
        {
            errors.Add("roomId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SourceClientId))
        {
            errors.Add("sourceClientId is required.");
        }

        if (!Enum.IsDefined(command.Type))
        {
            errors.Add("type is invalid.");
        }

        if (!Enum.IsDefined(command.SourceRole))
        {
            errors.Add("sourceRole is invalid.");
        }

        if (!ProtocolVersions.IsSupported(command.ProtocolVersion))
        {
            errors.Add($"protocolVersion '{command.ProtocolVersion}' is not supported.");
        }

        if (command.EmittedAt == default)
        {
            errors.Add("emittedAt is required.");
        }

        ValidatePayload(command, errors);

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Failure(errors.ToArray());
    }

    public static ValidationResult ValidateSnapshot(RoomSnapshot snapshot)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(snapshot.RoomId))
        {
            errors.Add("roomId is required.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.HostId))
        {
            errors.Add("hostId is required.");
        }

        if (snapshot.Revision < 0)
        {
            errors.Add("revision cannot be negative.");
        }

        if (snapshot.Playback.PositionMs < 0)
        {
            errors.Add("playback.positionMs cannot be negative.");
        }

        foreach (var participant in snapshot.Participants)
        {
            if (string.IsNullOrWhiteSpace(participant.ClientId))
            {
                errors.Add("participants[].clientId is required.");
            }

            if (!Enum.IsDefined(participant.Role))
            {
                errors.Add("participants[].role is invalid.");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Failure(errors.ToArray());
    }

    private static void ValidatePayload(RoomCommand command, ICollection<string> errors)
    {
        if (command.Payload.ValueKind == JsonValueKind.Undefined || command.Payload.ValueKind == JsonValueKind.Null)
        {
            errors.Add("payload must be a JSON object.");
            return;
        }

        if (command.Payload.ValueKind != JsonValueKind.Object)
        {
            errors.Add("payload must be a JSON object.");
            return;
        }

        switch (command.Type)
        {
            case CommandType.SEEK:
                ValidateSeekPayload(command.Payload, errors);
                break;
            case CommandType.READY:
                if (!TryGetProperty(command.Payload, "isReady", out var isReady) || isReady.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    errors.Add("payload.isReady must be a boolean for READY.");
                }
                break;
            case CommandType.PLAYER_CHANGED:
                if (!TryGetProperty(command.Payload, "playerClientId", out var playerClientId) || string.IsNullOrWhiteSpace(playerClientId.GetString()))
                {
                    errors.Add("payload.playerClientId is required for PLAYER_CHANGED.");
                }
                break;
            case CommandType.PLAY:
            case CommandType.PAUSE:
            case CommandType.RESYNC_REQUEST:
                break;
            default:
                errors.Add("type is invalid.");
                break;
        }
    }

    private static void ValidateSeekPayload(JsonElement payload, ICollection<string> errors)
    {
        if (!TryGetProperty(payload, "positionMs", out var positionMs) || positionMs.ValueKind != JsonValueKind.Number || !positionMs.TryGetInt64(out var parsed))
        {
            errors.Add("payload.positionMs must be a number for SEEK.");
            return;
        }

        if (parsed < 0)
        {
            errors.Add("payload.positionMs cannot be negative for SEEK.");
        }
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
