using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;
using StoryMixer.Api.Features.Auth;
using StoryMixer.Api.Features.Health;
using StoryMixer.Api.Features.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddValidation();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Missing '{JwtOptions.SectionName}' configuration.");

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();

builder.Services.AddSingleton(
    builder.Configuration.GetSection(RefreshTokenOptions.SectionName).Get<RefreshTokenOptions>()
        ?? new RefreshTokenOptions());
builder.Services.AddSingleton<RefreshTokenCookie>();
builder.Services.AddScoped<RefreshTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = TokenService.CreateSigningKey(jwtOptions.Key),
        };
    });
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

await app.RunAsync();
