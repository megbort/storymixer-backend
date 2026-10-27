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
        auth.MapPost("/refresh", Refresh);
        auth.MapPost("/logout", Logout);

        return app;
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        HttpResponse response,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        TokenService tokenService,
        RefreshTokenService refreshTokens,
        RefreshTokenCookie refreshTokenCookie)
    {
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == request.Email);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        refreshTokenCookie.Append(response, await refreshTokens.IssueAsync(user.Id));

        return Results.Ok(tokenService.CreateAccessToken(user));
    }

    private static async Task<IResult> Refresh(
        HttpRequest request,
        HttpResponse response,
        TokenService tokenService,
        RefreshTokenService refreshTokens,
        RefreshTokenCookie refreshTokenCookie)
    {
        var cookieValue = RefreshTokenCookie.Read(request);
        var current = cookieValue is null ? null : await refreshTokens.FindActiveAsync(cookieValue);

        if (current is null)
        {
            refreshTokenCookie.Delete(response);
            return Results.Unauthorized();
        }

        await refreshTokens.RevokeAsync(current);
        refreshTokenCookie.Append(response, await refreshTokens.IssueAsync(current.UserId));

        return Results.Ok(tokenService.CreateAccessToken(current.User));
    }

    private static async Task<IResult> Logout(
        HttpRequest request,
        HttpResponse response,
        RefreshTokenService refreshTokens,
        RefreshTokenCookie refreshTokenCookie)
    {
        var cookieValue = RefreshTokenCookie.Read(request);
        var current = cookieValue is null ? null : await refreshTokens.FindActiveAsync(cookieValue);

        if (current is not null)
        {
            await refreshTokens.RevokeAsync(current);
        }

        refreshTokenCookie.Delete(response);

        return Results.NoContent();
    }
}
