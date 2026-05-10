namespace PlayPair.Server.Observability;

public interface IServerTelemetry
{
    void RoomJoined();

    void RoomLeft();

    void CommandRelayed();

    void CommandRejected(string reason);

    void CommandDuplicate();

    void Reconnected();
}
