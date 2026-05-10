namespace PlayPair.Server.Rooms;

public sealed class RoomOperationException : Exception
{
    public RoomOperationException(string message, string code = "room_operation_failed")
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
