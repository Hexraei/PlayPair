using System.Text.Json;
using System.Text.RegularExpressions;
using PlayPair.Contracts.Models;
using PlayPair.Contracts.Validation;

namespace PlayPair.Server.Rooms;

public sealed partial class InMemoryRoomManager : IRoomManager
{
    private const int MaxParticipants = 2;
    private static readonly TimeSpan DisconnectRetentionWindow = TimeSpan.FromMinutes(2);
    private const int ProcessedCommandHistoryLimit = 256;
    private const int MaxRoomsLimit = 1000;

    private readonly object _syncRoot = new();
    private readonly Dictionary<string, RoomState> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _connectionToRoom = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryRoomManager> _logger;

    public InMemoryRoomManager(ILogger<InMemoryRoomManager> logger)
    {
        _logger = logger;
    }

    public RoomJoinResult CreateRoom(string connectionId, string displayName)
    {
        ValidateConnectionId(connectionId);
        ValidateDisplayName(displayName);

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            EnsureConnectionNotAlreadyInRoom(connectionId);

            if (_rooms.Count >= MaxRoomsLimit)
            {
                throw new RoomOperationException("Server room capacity reached.", "server_capacity_reached");
            }

            var roomCode = CreateUniqueRoomCode();
            var participant = ParticipantState.CreateConnected(connectionId, displayName, SourceRole.HOST);
            var state = RoomState.Create(roomCode, participant);

            _rooms[roomCode] = state;
            _connectionToRoom[connectionId] = roomCode;

            _logger.LogInformation(
                "RoomCreated {RoomCode} host {ClientId}",
                roomCode,
                participant.ClientId);

            return new RoomJoinResult(roomCode, participant.ClientId, participant.Role, ToSnapshot(state));
        }
    }

    public RoomJoinResult JoinRoom(string connectionId, string roomCode, string displayName)
    {
        ValidateConnectionId(connectionId);
        ValidateDisplayName(displayName);
        var normalizedRoomCode = NormalizeRoomCode(roomCode);

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            EnsureConnectionNotAlreadyInRoom(connectionId);

            if (!_rooms.TryGetValue(normalizedRoomCode, out var room))
            {
                throw new RoomOperationException("Room not found.", "room_not_found");
            }

            if (room.Participants.Count >= MaxParticipants)
            {
                throw new RoomOperationException("Room is full.", "room_full");
            }

            var participant = ParticipantState.CreateConnected(connectionId, displayName, SourceRole.GUEST);
            room.Participants.Add(participant);
            room.Revision += 1;
            room.LastCommandId = "system_join";
            _connectionToRoom[connectionId] = room.Code;

            _logger.LogInformation(
                "RoomJoined {RoomCode} guest {ClientId}",
                room.Code,
                participant.ClientId);

            return new RoomJoinResult(room.Code, participant.ClientId, participant.Role, ToSnapshot(room));
        }
    }

    public RoomJoinResult Reconnect(string connectionId, string roomCode, string clientId)
    {
        ValidateConnectionId(connectionId);
        ValidateClientId(clientId);
        var normalizedRoomCode = NormalizeRoomCode(roomCode);

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            EnsureConnectionNotAlreadyInRoom(connectionId);

            if (!_rooms.TryGetValue(normalizedRoomCode, out var room))
            {
                throw new RoomOperationException("Room not found.", "room_not_found");
            }

            var participant = room.Participants.SingleOrDefault(p => string.Equals(p.ClientId, clientId, StringComparison.Ordinal));
            if (participant is null)
            {
                throw new RoomOperationException("Client is not part of this room.", "client_not_in_room");
            }

            if (participant.IsConnected)
            {
                throw new RoomOperationException("Client is already connected.", "client_already_connected");
            }

            participant.ConnectionId = connectionId;
            participant.DisconnectedAt = null;
            participant.IsConnected = true;
            _connectionToRoom[connectionId] = room.Code;
            room.Revision += 1;
            room.LastCommandId = "system_reconnect";

            _logger.LogInformation(
                "ParticipantReconnected {RoomCode} {ClientId}",
                room.Code,
                participant.ClientId);

            return new RoomJoinResult(room.Code, participant.ClientId, participant.Role, ToSnapshot(room));
        }
    }

    public RoomLeaveResult LeaveRoom(string connectionId)
    {
        ValidateConnectionId(connectionId);

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
            {
                throw new RoomOperationException("Connection is not in a room.", "connection_not_in_room");
            }

            return LeaveRoomInternal(connectionId, roomCode);
        }
    }

    public RoomLeaveResult? LeaveRoomIfConnected(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return null;
        }

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
            {
                return null;
            }

            return LeaveRoomInternal(connectionId, roomCode);
        }
    }

    public RoomLeaveResult? MarkDisconnected(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return null;
        }

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
            {
                return null;
            }

            if (!_rooms.TryGetValue(roomCode, out var room))
            {
                _connectionToRoom.Remove(connectionId);
                return null;
            }

            var participant = room.Participants.SingleOrDefault(p => string.Equals(p.ConnectionId, connectionId, StringComparison.Ordinal));
            if (participant is null)
            {
                _connectionToRoom.Remove(connectionId);
                return null;
            }

            participant.IsConnected = false;
            participant.DisconnectedAt = DateTimeOffset.UtcNow;
            _connectionToRoom.Remove(connectionId);
            room.Revision += 1;
            room.LastCommandId = "system_disconnect";

            _logger.LogInformation(
                "ParticipantDisconnected {RoomCode} {ClientId}",
                room.Code,
                participant.ClientId);

            return new RoomLeaveResult(room.Code, false, ToSnapshot(room));
        }
    }

    public RoomSnapshot RecoverState(string connectionId, string roomCode, string clientId)
    {
        ValidateConnectionId(connectionId);
        ValidateClientId(clientId);
        var normalizedRoomCode = NormalizeRoomCode(roomCode);

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            if (!_connectionToRoom.TryGetValue(connectionId, out var mappedRoomCode) ||
                !string.Equals(mappedRoomCode, normalizedRoomCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new RoomOperationException("Connection is not in the requested room.", "connection_not_in_room");
            }

            if (!_rooms.TryGetValue(normalizedRoomCode, out var room))
            {
                throw new RoomOperationException("Room not found.", "room_not_found");
            }

            var participant = room.Participants.SingleOrDefault(p =>
                string.Equals(p.ConnectionId, connectionId, StringComparison.Ordinal) &&
                string.Equals(p.ClientId, clientId, StringComparison.Ordinal));
            if (participant is null)
            {
                throw new RoomOperationException("Participant not found for state recovery.", "participant_not_found");
            }

            return ToSnapshot(room);
        }
    }

    public CommandRelayResult ProcessCommand(string connectionId, RoomCommand command)
    {
        ValidateConnectionId(connectionId);
        if (command is null)
        {
            throw new RoomOperationException("command is required.", "invalid_command");
        }

        lock (_syncRoot)
        {
            PruneExpiredDisconnectedParticipantsLocked();
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
            {
                throw new RoomOperationException("Connection is not in a room.", "connection_not_in_room");
            }

            if (!_rooms.TryGetValue(roomCode, out var room))
            {
                throw new RoomOperationException("Room not found.", "room_not_found");
            }

            var participant = room.Participants.SingleOrDefault(p =>
                string.Equals(p.ConnectionId, connectionId, StringComparison.Ordinal) &&
                p.IsConnected);
            if (participant is null)
            {
                throw new RoomOperationException("Participant not found.", "participant_not_found");
            }

            if (!string.Equals(command.RoomId, room.Code, StringComparison.OrdinalIgnoreCase))
            {
                throw new RoomOperationException("Command roomId does not match participant room.", "invalid_room_for_command");
            }

            if (!string.Equals(command.SourceClientId, participant.ClientId, StringComparison.Ordinal))
            {
                throw new RoomOperationException("sourceClientId does not match sender.", "invalid_source_client");
            }

            if (command.SourceRole != participant.Role)
            {
                throw new RoomOperationException("sourceRole does not match sender role.", "invalid_source_role");
            }

            var validationResult = ContractValidator.ValidateCommand(command);
            if (!validationResult.IsValid)
            {
                throw new RoomOperationException(
                    $"Command validation failed: {string.Join("; ", validationResult.Errors)}",
                    "invalid_command");
            }

            var fingerprint = ComputeFingerprint(command);
            if (room.ProcessedCommands.TryGetValue(command.CommandId, out var priorResult))
            {
                if (!string.Equals(priorResult.Fingerprint, fingerprint, StringComparison.Ordinal))
                {
                    throw new RoomOperationException("Duplicate commandId with different payload is not allowed.", "duplicate_command_conflict");
                }

                return new CommandRelayResult(
                    room.Code,
                    priorResult.Command,
                    priorResult.Snapshot,
                    CommandProcessStatus.Duplicate,
                    null);
            }

            if (IsMutatingCommand(command.Type) && command.Type != CommandType.READY && participant.Role != SourceRole.HOST)
            {
                throw new RoomOperationException("Only host can mutate room state for this command.", "host_required");
            }

            RoomSnapshot? snapshot = null;
            if (IsMutatingCommand(command.Type))
            {
                if (command.EmittedAt < room.LastMutatingCommandEmittedAt)
                {
                    throw new RoomOperationException("Stale command rejected due to out-of-order emission time.", "stale_command");
                }

                ApplyMutatingCommand(room, participant, command);
                room.LastMutatingCommandEmittedAt = command.EmittedAt;
                snapshot = ToSnapshot(room);
            }

            RegisterProcessedCommand(room, command, snapshot, fingerprint);

            _logger.LogInformation(
                "CommandAccepted {RoomCode} {CommandType} by {Role} {ClientId}",
                room.Code,
                command.Type,
                participant.Role,
                participant.ClientId);

            return new CommandRelayResult(room.Code, command, snapshot, CommandProcessStatus.Accepted, null);
        }
    }

    private static bool IsMutatingCommand(CommandType type)
        => type is CommandType.PLAY
            or CommandType.PAUSE
            or CommandType.SEEK
            or CommandType.READY
            or CommandType.PLAYER_CHANGED;

    private static void ValidateConnectionId(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new RoomOperationException("connectionId is required.", "invalid_connection_id");
        }
    }

    private static void ValidateClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new RoomOperationException("clientId is required.", "invalid_client_id");
        }
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new RoomOperationException("displayName is required.", "invalid_display_name");
        }

        if (displayName.Trim().Length > 40)
        {
            throw new RoomOperationException("displayName cannot exceed 40 characters.", "invalid_display_name");
        }
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new RoomOperationException("roomCode is required.", "invalid_room_code");
        }

        var normalized = roomCode.Trim().ToUpperInvariant();
        if (!RoomCodeRegex().IsMatch(normalized))
        {
            throw new RoomOperationException("roomCode must be 6 alphanumeric uppercase characters.", "invalid_room_code");
        }

        return normalized;
    }

    private string CreateUniqueRoomCode()
    {
        const int maxAttempts = 100;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = RoomCodeGenerator.CreateCode();
            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }

        throw new RoomOperationException("Unable to allocate unique room code.", "room_code_allocation_failed");
    }

    private static RoomSnapshot ToSnapshot(RoomState state)
        => new()
        {
            RoomId = state.Code,
            HostId = state.Participants.Single(p => p.Role == SourceRole.HOST).ClientId,
            Participants = state.Participants
                .Select(p => new ParticipantSnapshot
                {
                    ClientId = p.ClientId,
                    DisplayName = p.DisplayName,
                    Role = p.Role,
                    IsReady = p.IsReady
                })
                .ToArray(),
            Playback = new PlaybackSnapshot
            {
                Status = state.PlaybackStatus,
                PositionMs = state.PositionMs,
                Title = state.Title
            },
            Revision = state.Revision,
            LastCommandId = state.LastCommandId
        };

    private void EnsureConnectionNotAlreadyInRoom(string connectionId)
    {
        if (_connectionToRoom.ContainsKey(connectionId))
        {
            throw new RoomOperationException("Connection is already in a room.", "connection_already_in_room");
        }
    }

    private RoomLeaveResult LeaveRoomInternal(string connectionId, string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var room))
        {
            _connectionToRoom.Remove(connectionId);
            throw new RoomOperationException("Room not found.", "room_not_found");
        }

        var removedParticipant = room.Participants.SingleOrDefault(p => string.Equals(p.ConnectionId, connectionId, StringComparison.Ordinal));
        if (removedParticipant is null)
        {
            _connectionToRoom.Remove(connectionId);
            throw new RoomOperationException("Participant not found.", "participant_not_found");
        }

        room.Participants.Remove(removedParticipant);
        _connectionToRoom.Remove(connectionId);

        if (room.Participants.Count == 0)
        {
            _rooms.Remove(room.Code);
            _logger.LogInformation("RoomClosed {RoomCode}", room.Code);
            return new RoomLeaveResult(room.Code, true, null);
        }

        PromoteHostIfNeeded(room, removedParticipant.Role);

        room.Revision += 1;
        room.LastCommandId = "system_leave";
        var snapshot = ToSnapshot(room);
        _logger.LogInformation(
            "ParticipantLeft {RoomCode} {ClientId}",
            room.Code,
            removedParticipant.ClientId);
        return new RoomLeaveResult(room.Code, false, snapshot);
    }

    private static void ApplyMutatingCommand(RoomState room, ParticipantState participant, RoomCommand command)
    {
        switch (command.Type)
        {
            case CommandType.PLAY:
                room.PlaybackStatus = PlaybackStatus.PLAYING;
                break;
            case CommandType.PAUSE:
                room.PlaybackStatus = PlaybackStatus.PAUSED;
                break;
            case CommandType.SEEK:
                room.PositionMs = command.Payload.GetProperty("positionMs").GetInt64();
                break;
            case CommandType.READY:
                participant.IsReady = command.Payload.GetProperty("isReady").GetBoolean();
                break;
            case CommandType.PLAYER_CHANGED:
                room.Title = command.Payload.TryGetProperty("title", out var title)
                    ? title.GetString() ?? room.Title
                    : room.Title;
                break;
        }

        room.Revision += 1;
        room.LastCommandId = command.CommandId;
    }

    private static void PromoteHostIfNeeded(RoomState room, SourceRole removedRole)
    {
        if (removedRole != SourceRole.HOST)
        {
            return;
        }

        var promoted = room.Participants[0];
        promoted.Role = SourceRole.HOST;
    }

    private static string ComputeFingerprint(RoomCommand command)
        => JsonSerializer.Serialize(command);

    private static void RegisterProcessedCommand(RoomState room, RoomCommand command, RoomSnapshot? snapshot, string fingerprint)
    {
        room.ProcessedCommands[command.CommandId] = new ProcessedCommandRecord(command, snapshot, fingerprint);
        room.ProcessedCommandOrder.Enqueue(command.CommandId);

        while (room.ProcessedCommandOrder.Count > ProcessedCommandHistoryLimit)
        {
            var oldest = room.ProcessedCommandOrder.Dequeue();
            room.ProcessedCommands.Remove(oldest);
        }
    }

    private void PruneExpiredDisconnectedParticipantsLocked()
    {
        var now = DateTimeOffset.UtcNow;
        var roomsToDelete = new List<string>();

        foreach (var room in _rooms.Values)
        {
            var expiredParticipants = room.Participants
                .Where(p => !p.IsConnected && p.DisconnectedAt is not null && now - p.DisconnectedAt.Value > DisconnectRetentionWindow)
                .ToList();
            if (expiredParticipants.Count == 0)
            {
                continue;
            }

            var removedHost = false;
            foreach (var participant in expiredParticipants)
            {
                removedHost = removedHost || participant.Role == SourceRole.HOST;
                room.Participants.Remove(participant);
                if (participant.ConnectionId is not null)
                {
                    _connectionToRoom.Remove(participant.ConnectionId);
                }
            }

            if (room.Participants.Count == 0)
            {
                roomsToDelete.Add(room.Code);
                continue;
            }

            if (removedHost)
            {
                var promoted = room.Participants[0];
                promoted.Role = SourceRole.HOST;
                _logger.LogInformation(
                    "HostPromotedAfterDisconnectTimeout {RoomCode} {ClientId}",
                    room.Code,
                    promoted.ClientId);
            }

            room.Revision += 1;
            room.LastCommandId = "system_disconnect_timeout";
        }

        foreach (var roomCode in roomsToDelete)
        {
            _rooms.Remove(roomCode);
            _logger.LogInformation("RoomClosedAfterDisconnectTimeout {RoomCode}", roomCode);
        }
    }

    [GeneratedRegex("^[A-Z0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex RoomCodeRegex();

    private sealed class RoomState
    {
        public required string Code { get; init; }

        public required List<ParticipantState> Participants { get; init; }

        public PlaybackStatus PlaybackStatus { get; set; } = PlaybackStatus.PAUSED;

        public long PositionMs { get; set; }

        public string Title { get; set; } = string.Empty;

        public long Revision { get; set; }

        public string LastCommandId { get; set; } = string.Empty;

        public DateTimeOffset LastMutatingCommandEmittedAt { get; set; } = DateTimeOffset.MinValue;

        public Dictionary<string, ProcessedCommandRecord> ProcessedCommands { get; } = new(StringComparer.Ordinal);

        public Queue<string> ProcessedCommandOrder { get; } = new();

        public static RoomState Create(string code, ParticipantState host)
            => new()
            {
                Code = code,
                Participants = [host]
            };
    }

    private sealed record ProcessedCommandRecord(RoomCommand Command, RoomSnapshot? Snapshot, string Fingerprint);

    private sealed class ParticipantState
    {
        private ParticipantState(string connectionId, string clientId, string displayName, SourceRole role)
        {
            ConnectionId = connectionId;
            ClientId = clientId;
            DisplayName = displayName;
            Role = role;
        }

        public string? ConnectionId { get; set; }

        public string ClientId { get; }

        public string DisplayName { get; }

        public SourceRole Role { get; set; }

        public bool IsReady { get; set; }

        public bool IsConnected { get; set; }

        public DateTimeOffset? DisconnectedAt { get; set; }

        public static ParticipantState CreateConnected(string connectionId, string displayName, SourceRole role)
            => new(
                connectionId,
                Guid.NewGuid().ToString("N"),
                displayName.Trim(),
                role)
            {
                IsConnected = true
            };
    }
}
