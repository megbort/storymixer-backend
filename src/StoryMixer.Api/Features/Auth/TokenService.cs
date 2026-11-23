using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Features.Auth;

public class TokenService(JwtOptions options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler _handler = new();

    public static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));

    public AccessTokenResponse CreateAccessToken(User user)
    {
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(options.AccessTokenMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
            ]),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(options.Key),
                SecurityAlgorithms.HmacSha256),
        });

        return new AccessTokenResponse(token, expiresAt);
    }
}
