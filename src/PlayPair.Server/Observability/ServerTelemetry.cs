using System.Diagnostics.Metrics;

namespace PlayPair.Server.Observability;

public sealed class ServerTelemetry : IServerTelemetry
{
    private readonly Counter<long> _joins;
    private readonly Counter<long> _leaves;
    private readonly Counter<long> _relayedCommands;
    private readonly Counter<long> _rejectedCommands;
    private readonly Counter<long> _duplicateCommands;
    private readonly Counter<long> _reconnects;

    public ServerTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("PlayPair.Server.Rooms");
        _joins = meter.CreateCounter<long>("playpair.rooms.joins");
        _leaves = meter.CreateCounter<long>("playpair.rooms.leaves");
        _relayedCommands = meter.CreateCounter<long>("playpair.rooms.commands_relayed");
        _rejectedCommands = meter.CreateCounter<long>("playpair.rooms.commands_rejected");
        _duplicateCommands = meter.CreateCounter<long>("playpair.rooms.commands_duplicate");
        _reconnects = meter.CreateCounter<long>("playpair.rooms.reconnects");
    }

    public void RoomJoined() => _joins.Add(1);

    public void RoomLeft() => _leaves.Add(1);

    public void CommandRelayed() => _relayedCommands.Add(1);

    public void CommandRejected(string reason) => _rejectedCommands.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void CommandDuplicate() => _duplicateCommands.Add(1);

    public void Reconnected() => _reconnects.Add(1);
}
