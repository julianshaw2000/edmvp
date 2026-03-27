namespace Tungsten.Api.Common.Auth;

public class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public required string IdentityUserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}
