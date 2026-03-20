namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class RiskCountryEntity
{
    public required string CountryCode { get; set; }
    public required string CountryName { get; set; }
    public required string RiskLevel { get; set; }
    public required string Source { get; set; }
}
