namespace StoryMixer.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<Story> Stories { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
