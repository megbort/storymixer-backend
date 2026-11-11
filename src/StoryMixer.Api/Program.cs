using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;
using StoryMixer.Api.Features.Health;
using StoryMixer.Api.Features.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

// Outside Development (e.g. Railway), apply pending migrations on startup.
// Locally, migrations are applied with `dotnet ef database update`.
if (!app.Environment.IsDevelopment())
{
  using var scope = app.Services.CreateScope();
  await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "StoryMixer API"));
}

app.UseHttpsRedirection();

app.MapHealthEndpoints();
app.MapUserEndpoints();

await app.RunAsync();
