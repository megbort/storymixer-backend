using System.ComponentModel.DataAnnotations;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Features.Users;

public record CreateUserRequest(
    [property: Required, MaxLength(100)] string Name,
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Password);

public record UserResponse(Guid Id, string Name, string Email, DateTimeOffset CreatedAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}
