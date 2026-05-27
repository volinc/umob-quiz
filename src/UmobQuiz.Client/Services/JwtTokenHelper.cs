using System.Text;
using System.Text.Json;

namespace UmobQuiz.Client.Services;

internal static class JwtTokenHelper
{
    public static bool IsExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return true;
            }

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("exp", out var expElement))
            {
                return true;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
            return expiresAt <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
