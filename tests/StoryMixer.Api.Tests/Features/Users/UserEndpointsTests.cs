using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;
using StoryMixer.Api.Features.Auth;
using StoryMixer.Api.Features.Users;

namespace StoryMixer.Api.Tests.Features.Users;

public class UserEndpointsTests(StoryMixerApiFactory factory) : IClassFixture<StoryMixerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CreateUserRequest NewUserRequest(string? email = null) =>
        new("Ada Lovelace", email ?? $"ada-{Guid.NewGuid()}@example.com", "correct-horse-battery");

    [Fact]
    public async Task CreateUser_ReturnsCreatedUserWithLocationOfCurrentUser()
    {
        var request = NewUserRequest();

        var response = await _client.PostAsJsonAsync("/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(request.Email, user.Email);
        Assert.Equal("/users/me", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateUser_ResponseDoesNotContainPassword()
    {
        var response = await _client.PostAsJsonAsync("/users", NewUserRequest());

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateUser_StoresHashedPassword()
    {
        var request = NewUserRequest();

        await _client.PostAsJsonAsync("/users", request);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var stored = await db.Users.SingleAsync(user => user.Email == request.Email);
        Assert.NotEqual(request.Password, stored.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(stored, stored.PasswordHash, request.Password));
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmailInDifferentCase_ReturnsConflict()
    {
        var request = NewUserRequest();
        await _client.PostAsJsonAsync("/users", request);

        var duplicate = request with { Email = request.Email.ToUpperInvariant() };
        var response = await _client.PostAsJsonAsync("/users", duplicate);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "ada@example.com", "correct-horse-battery")]
    [InlineData("Ada", "not-an-email", "correct-horse-battery")]
    [InlineData("Ada", "ada@example.com", "short")]
    public async Task CreateUser_WithInvalidInput_ReturnsBadRequest(string name, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserRequest(name, email, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithToken_ReturnsLoggedInUser()
    {
        var request = NewUserRequest();
        var created = await (await _client.PostAsJsonAsync("/users", request))
            .Content.ReadFromJsonAsync<UserResponse>();
        var login = await (await _client.PostAsJsonAsync("/auth/login", new LoginRequest(request.Email, request.Password)))
            .Content.ReadFromJsonAsync<AccessTokenResponse>();

        using var message = new HttpRequestMessage(HttpMethod.Get, "/users/me");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var response = await _client.SendAsync(message);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.Content.ReadFromJsonAsync<UserResponse>());
    }
}
