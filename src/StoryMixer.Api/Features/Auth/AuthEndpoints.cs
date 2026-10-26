using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/login", Login);

        return app;
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        TokenService tokenService)
    {
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == request.Email);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(tokenService.CreateAccessToken(user));
    }
}
