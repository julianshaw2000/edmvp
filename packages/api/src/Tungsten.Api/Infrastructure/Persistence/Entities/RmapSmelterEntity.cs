namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class RmapSmelterEntity
{
    public required string SmelterId { get; set; }
    public required string SmelterName { get; set; }
    public required string Country { get; set; }
    public required string ConformanceStatus { get; set; }
    public DateOnly? LastAuditDate { get; set; }
    public DateTime LoadedAt { get; set; }
}
