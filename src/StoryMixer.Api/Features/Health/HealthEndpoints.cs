using StoryMixer.Api.Data;

namespace StoryMixer.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/db", async (AppDbContext db) =>
            await db.Database.CanConnectAsync()
                ? Results.Ok("db ok")
                : Results.Problem("db unreachable"));

        return app;
    }
}
