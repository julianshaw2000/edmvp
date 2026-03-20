namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class SanctionedEntityEntity
{
    public Guid Id { get; set; }
    public required string EntityName { get; set; }
    public required string EntityType { get; set; }
    public required string Source { get; set; }
    public DateTime LoadedAt { get; set; }
}
