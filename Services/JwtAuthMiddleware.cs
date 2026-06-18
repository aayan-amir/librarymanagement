using System.Security.Claims;

namespace QrDigitalLibrary.Api.Services;

public sealed class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && jwtTokenService.TryValidateToken(authorization["Bearer ".Length..].Trim(), out var user)
            && user is not null)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UniversityId),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("university_id", user.UniversityId)
            };

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "QrJwt"));
        }

        await _next(context);
    }
}
