using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(string identityUserId, string email);
    string GenerateRefreshToken();
    string HashToken(string token);
    Task<RefreshTokenEntity> SaveRefreshTokenAsync(string identityUserId, string refreshToken, CancellationToken ct);
    Task<RefreshTokenEntity?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task RevokeRefreshTokenAsync(string tokenHash, CancellationToken ct);
    Task RevokeAllUserTokensAsync(string identityUserId, CancellationToken ct);
}

public class JwtTokenService(IConfiguration config, AppDbContext db) : IJwtTokenService
{
    public string GenerateAccessToken(string identityUserId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, identityUserId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashToken(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<RefreshTokenEntity> SaveRefreshTokenAsync(string identityUserId, string refreshToken, CancellationToken ct)
    {
        var entity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityUserId,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
        };
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<RefreshTokenEntity?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        return await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow, ct);
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(r => r.TokenHash == tokenHash)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
    }

    public async Task RevokeAllUserTokensAsync(string identityUserId, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(r => r.IdentityUserId == identityUserId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
    }
}
