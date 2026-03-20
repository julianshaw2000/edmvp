namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class NotificationEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public bool EmailSent { get; set; }
    public int EmailRetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}
