namespace QrDigitalLibrary.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(string universityId, string fullName, string role, out DateTimeOffset expiresAt);

    bool TryValidateToken(string token, out AuthenticatedUser? user);
}
