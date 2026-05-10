using PlayPair.Contracts.Models;

namespace PlayPair.Server.Rooms;

public interface IRoomManager
{
    RoomJoinResult CreateRoom(string connectionId, string displayName);

    RoomJoinResult JoinRoom(string connectionId, string roomCode, string displayName);

    RoomJoinResult Reconnect(string connectionId, string roomCode, string clientId);

    RoomLeaveResult LeaveRoom(string connectionId);

    RoomLeaveResult? LeaveRoomIfConnected(string connectionId);

    CommandRelayResult ProcessCommand(string connectionId, RoomCommand command);

    RoomLeaveResult? MarkDisconnected(string connectionId);

    RoomSnapshot RecoverState(string connectionId, string roomCode, string clientId);
}
