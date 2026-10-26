namespace StoryMixer.Api.Features.Auth;

public record JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public int AccessTokenMinutes { get; init; } = 15;
}
