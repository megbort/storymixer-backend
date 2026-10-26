using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/users").WithTags("Users");

        users.MapPost("/", CreateUser);
        users.MapGet("/me", GetCurrentUser).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher)
    {
        if (await db.Users.AnyAsync(user => user.Email == request.Email))
        {
            return Results.Conflict(new { error = "A user with this email already exists." });
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = string.Empty,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Created("/users/me", UserResponse.From(user));
    }

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal principal, AppDbContext db)
    {
        if (!Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.FindAsync(userId);

        return user is null ? Results.Unauthorized() : Results.Ok(UserResponse.From(user));
    }
}
