using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/users").WithTags("Users");

        users.MapPost("/", CreateUser);
        users.MapGet("/", GetUsers);
        users.MapGet("/{id:guid}", GetUserById).WithName(nameof(GetUserById));

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

        return Results.CreatedAtRoute(nameof(GetUserById), new { id = user.Id }, UserResponse.From(user));
    }

    private static async Task<IResult> GetUsers(AppDbContext db)
    {
        var users = await db.Users
            .OrderBy(user => user.CreatedAt)
            .Select(user => new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt))
            .ToListAsync();

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserById(Guid id, AppDbContext db)
    {
        var user = await db.Users.FindAsync(id);

        return user is null ? Results.NotFound() : Results.Ok(UserResponse.From(user));
    }
}
