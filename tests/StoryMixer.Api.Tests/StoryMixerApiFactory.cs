using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoryMixer.Api.Data;
using Testcontainers.PostgreSql;

namespace StoryMixer.Api.Tests;

public class StoryMixerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AllowedOrigin = "https://app.storymixer.test";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => _database.DisposeAsync().AsTask();
}
