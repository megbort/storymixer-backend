using System.Net;
using System.Net.Http.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using StoryMixer.Api.Features.Auth;
using StoryMixer.Api.Features.Users;

namespace StoryMixer.Api.Tests.Features.Auth;

public class AuthEndpointsTests(StoryMixerApiFactory factory) : IClassFixture<StoryMixerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<UserResponse> CreateUserAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserRequest("Ada Lovelace", email, password));
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    private static string NewEmail() => $"ada-{Guid.NewGuid()}@example.com";

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
}
