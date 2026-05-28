using System.Security.Cryptography;

namespace PlayPair.Server.Rooms;

public static class RoomCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int RoomCodeLength = 6;

    public static string CreateCode()
    {
        return string.Create(RoomCodeLength, Alphabet, (span, alphabetState) =>
        {
            for (var index = 0; index < span.Length; index++)
            {
                span[index] = alphabetState[RandomNumberGenerator.GetInt32(alphabetState.Length)];
            }
        });
    }
}
