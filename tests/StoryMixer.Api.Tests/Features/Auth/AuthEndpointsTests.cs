using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
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
}
