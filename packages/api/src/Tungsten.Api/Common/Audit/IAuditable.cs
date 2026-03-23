namespace Tungsten.Api.Common.Audit;

public interface IAuditable
{
    string AuditAction { get; }
    string EntityType { get; }
}
