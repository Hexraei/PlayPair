namespace PlayPair.Server.Rooms;

public static class RoomCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int RoomCodeLength = 6;

    public static string CreateCode()
    {
        Span<char> chars = stackalloc char[RoomCodeLength];
        for (var index = 0; index < chars.Length; index++)
        {
            chars[index] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(chars);
    }
}
