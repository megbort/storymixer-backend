namespace StoryMixer.Api.Entities;

public class Story
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Result { get; set; }
    public required string[] Inputs { get; set; }
    public string? ImageKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
