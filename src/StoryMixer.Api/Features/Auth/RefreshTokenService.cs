using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StoryMixer.Api.Data;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Features.Auth;

public record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt);

public class RefreshTokenService(AppDbContext db, RefreshTokenOptions options, TimeProvider timeProvider)
{
    public async Task<IssuedRefreshToken> IssueAsync(Guid userId)
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = timeProvider.GetUtcNow().AddDays(options.LifetimeDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(value),
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync();

        return new IssuedRefreshToken(value, expiresAt);
    }

    public async Task<RefreshToken?> FindActiveAsync(string value)
    {
        var tokenHash = Hash(value);
        var now = timeProvider.GetUtcNow();

        return await db.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token =>
                token.TokenHash == tokenHash && token.RevokedAt == null && token.ExpiresAt > now);
    }

    public async Task RevokeAsync(RefreshToken token)
    {
        token.RevokedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync();
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
