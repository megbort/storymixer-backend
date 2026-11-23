using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using StoryMixer.Api.Data;
using StoryMixer.Api.Features.Auth;
using StoryMixer.Api.Features.Users;

namespace StoryMixer.Api.Tests.Features.Auth;

public class AuthEndpointsTests(StoryMixerApiFactory factory) : IClassFixture<StoryMixerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    private async Task<UserResponse> CreateUserAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserRequest("Ada Lovelace", email, password));
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    private static string NewEmail() => $"ada-{Guid.NewGuid()}@example.com";

    private static string GetRefreshSetCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(header => header.StartsWith("refresh_token="));

    private static string ToCookieHeader(string setCookie) => setCookie.Split(';')[0];

    private async Task<string> LoginAndGetRefreshCookieAsync()
    {
        var email = NewEmail();
        await CreateUserAsync(email, "correct-horse-battery");
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, "correct-horse-battery"));
        return ToCookieHeader(GetRefreshSetCookie(response));
    }

    private async Task<HttpResponseMessage> PostWithCookieAsync(string path, string? cookie)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path);
        if (cookie is not null)
        {
            message.Headers.Add("Cookie", cookie);
        }
        return await _client.SendAsync(message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessTokenForUser()
    {
        var email = NewEmail();
        var user = await CreateUserAsync(email, "correct-horse-battery");

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, "correct-horse-battery"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.NotNull(login);
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(login.AccessToken);
        Assert.Equal(user.Id.ToString(), token.Subject);
    }

    [Fact]
    public async Task Login_WithEmailInDifferentCase_Succeeds()
    {
        var email = NewEmail();
        await CreateUserAsync(email, "correct-horse-battery");

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email.ToUpperInvariant(), "correct-horse-battery"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = NewEmail();
        await CreateUserAsync(email, "correct-horse-battery");

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(NewEmail(), "correct-horse-battery"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_SetsHttpOnlySecureRefreshCookieScopedToAuth()
    {
        var email = NewEmail();
        await CreateUserAsync(email, "correct-horse-battery");

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, "correct-horse-battery"));

        var setCookie = GetRefreshSetCookie(response).ToLowerInvariant();
        Assert.Contains("httponly", setCookie);
        Assert.Contains("secure", setCookie);
        Assert.Contains("path=/auth", setCookie);
        Assert.Contains("samesite=lax", setCookie);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsAccessTokenAndRotatesCookie()
    {
        var cookie = await LoginAndGetRefreshCookieAsync();

        var response = await PostWithCookieAsync("/auth/refresh", cookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<AccessTokenResponse>());
        Assert.NotEqual(cookie, ToCookieHeader(GetRefreshSetCookie(response)));
    }

    [Fact]
    public async Task Refresh_WithAlreadyUsedCookie_ReturnsUnauthorized()
    {
        var cookie = await LoginAndGetRefreshCookieAsync();
        await PostWithCookieAsync("/auth/refresh", cookie);

        var response = await PostWithCookieAsync("/auth/refresh", cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await PostWithCookieAsync("/auth/refresh", cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var cookie = await LoginAndGetRefreshCookieAsync();

        var logout = await PostWithCookieAsync("/auth/logout", cookie);
        var refresh = await PostWithCookieAsync("/auth/refresh", cookie);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithExpiredCookie_ReturnsUnauthorized()
    {
        var cookie = await LoginAndGetRefreshCookieAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookie.Split('=', 2)[1])));
            await db.RefreshTokens
                .Where(token => token.TokenHash == tokenHash)
                .ExecuteUpdateAsync(setters =>
                setters.SetProperty(token => token.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        var response = await PostWithCookieAsync("/auth/refresh", cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_ReturnsNoContent()
    {
        var response = await PostWithCookieAsync("/auth/logout", cookie: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_AllowsCredentials()
    {
        var response = await SendPreflightAsync(StoryMixerApiFactory.AllowedOrigin);

        Assert.Equal(StoryMixerApiFactory.AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_IsNotAllowed()
    {
        var response = await SendPreflightAsync("https://evil.example.com");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Refresh_ConcurrentRequestsWithSameCookie_OnlyOneSucceeds()
    {
        var cookie = await LoginAndGetRefreshCookieAsync();

        var responses = await Task.WhenAll(
            PostWithCookieAsync("/auth/refresh", cookie),
            PostWithCookieAsync("/auth/refresh", cookie));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var rejected = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        Assert.False(SetsRefreshCookie(rejected));
    }

    [Fact]
    public async Task Refresh_WithRevokedCookie_DoesNotClearCookie()
    {
        var cookie = await LoginAndGetRefreshCookieAsync();
        await PostWithCookieAsync("/auth/refresh", cookie);

        var response = await PostWithCookieAsync("/auth/refresh", cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(SetsRefreshCookie(response));
    }

    private static bool SetsRefreshCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var headers) &&
        headers.Any(header => header.StartsWith($"{RefreshTokenCookie.Name}="));

    [Fact]
    public async Task RotateAsync_SameTokenLoadedTwice_OnlyFirstRotationSucceeds()
    {
        var cookieValue = (await LoginAndGetRefreshCookieAsync()).Split('=', 2)[1];
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<RefreshTokenService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<RefreshTokenService>();
        var firstLoad = await firstService.FindActiveAsync(cookieValue);
        var secondLoad = await secondService.FindActiveAsync(cookieValue);

        var firstRotation = await firstService.RotateAsync(firstLoad!);
        var secondRotation = await secondService.RotateAsync(secondLoad!);

        Assert.NotNull(firstRotation);
        Assert.Null(secondRotation);
    }

    private async Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        using var message = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        message.Headers.Add("Origin", origin);
        message.Headers.Add("Access-Control-Request-Method", "POST");
        message.Headers.Add("Access-Control-Request-Headers", "content-type");
        return await _client.SendAsync(message);
    }
}
