using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QrDigitalLibrary.Api.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(string universityId, string fullName, string role, out DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        expiresAt = now.AddMinutes(GetExpiryMinutes());

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["sub"] = universityId,
            ["name"] = fullName,
            ["role"] = role,
            ["iss"] = GetIssuer(),
            ["aud"] = GetAudience(),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds()
        };

        var unsignedToken = $"{Base64Url(JsonSerializer.SerializeToUtf8Bytes(header))}.{Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload))}";
        var signature = Sign(unsignedToken);
        return $"{unsignedToken}.{signature}";
    }

    public bool TryValidateToken(string token, out AuthenticatedUser? user)
    {
        user = null;
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        if (!FixedTimeEquals(parts[2], Sign(unsignedToken)))
        {
            return false;
        }

        try
        {
            using var payloadDoc = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var payload = payloadDoc.RootElement;
            if (payload.GetProperty("iss").GetString() != GetIssuer()
                || payload.GetProperty("aud").GetString() != GetAudience()
                || payload.GetProperty("exp").GetInt64() < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }

            user = new AuthenticatedUser
            {
                UniversityId = payload.GetProperty("sub").GetString() ?? string.Empty,
                FullName = payload.GetProperty("name").GetString() ?? string.Empty,
                Role = payload.GetProperty("role").GetString() ?? "Student"
            };

            return !string.IsNullOrWhiteSpace(user.UniversityId);
        }
        catch
        {
            return false;
        }
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(GetSecret()));
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private string GetSecret() =>
        _configuration["Jwt:Secret"]
        ?? Environment.GetEnvironmentVariable("QR_LIBRARY_JWT_SECRET")
        ?? "development-only-secret-change-before-deployment-32chars";

    private string GetIssuer() => _configuration["Jwt:Issuer"] ?? "QrDigitalLibrary";

    private string GetAudience() => _configuration["Jwt:Audience"] ?? "QrDigitalLibrary.Users";

    private int GetExpiryMinutes() =>
        int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 120;

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
