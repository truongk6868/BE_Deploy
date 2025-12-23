using System.Security.Cryptography;

public static class TokenHelper
{
    public static string GenerateCheckInToken(int bookingId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(4);
        var randomPart = BitConverter.ToString(randomBytes).Replace("-", "");
        return $"CK-{bookingId}-{randomPart}";
    }
}
