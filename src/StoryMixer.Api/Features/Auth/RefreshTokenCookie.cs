namespace StoryMixer.Api.Features.Auth;

public class RefreshTokenCookie(RefreshTokenOptions options)
{
    public const string Name = "refresh_token";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public void Append(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Value, CreateOptions(token.ExpiresAt));

    public void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, CreateOptions(expiresAt: null));

    private CookieOptions CreateOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = options.CookieSameSite,
        Path = "/auth",
        Expires = expiresAt,
    };
}
