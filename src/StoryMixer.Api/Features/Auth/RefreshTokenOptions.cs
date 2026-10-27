namespace StoryMixer.Api.Features.Auth;

public record RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int LifetimeDays { get; init; } = 14;
    public SameSiteMode CookieSameSite { get; init; } = SameSiteMode.Lax;
}
