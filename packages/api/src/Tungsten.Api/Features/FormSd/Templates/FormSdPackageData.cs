namespace Tungsten.Api.Features.FormSd.Templates;

public record FormSdPackageData(
    string TenantName, int ReportingYear, DateTime GeneratedAt, string GeneratedBy,
    string RuleSetVersion, string PlatformVersion,
    IReadOnlyList<BatchApplicability> Applicability,
    IReadOnlyList<BatchSupplyChain> SupplyChains,
    IReadOnlyList<BatchDueDiligence> DueDiligence,
    IReadOnlyList<BatchRiskAssessment> RiskAssessments);

public record BatchApplicability(string BatchNumber, string MineralType, string OriginCountry, string Status, string Reasoning);
public record BatchSupplyChain(string BatchNumber, string NarrativeText, int EventCount, int GapCount);
public record BatchDueDiligence(string BatchNumber, int FlagCount, string OecdVersion, string SummaryText, IReadOnlyList<string> SmelterNames);
public record BatchRiskAssessment(string BatchNumber, string OverallRating, IReadOnlyList<RiskCategoryItem> Categories);
public record RiskCategoryItem(string Category, string Rating, string Detail);
