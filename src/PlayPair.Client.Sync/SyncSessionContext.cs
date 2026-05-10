using PlayPair.Contracts.Models;

namespace PlayPair.Client.Sync;

public sealed record SyncSessionContext(string RoomId, string ClientId, SourceRole Role);
