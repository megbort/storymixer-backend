using System.ComponentModel.DataAnnotations;

namespace StoryMixer.Api.Features.Auth;

public record LoginRequest(
    [property: Required] string Email,
    [property: Required] string Password);

public record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
