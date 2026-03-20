using System.Text.Json;

namespace Tungsten.Api.Features.CustodyEvents;

public record MetadataValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static MetadataValidationResult Success() => new(true, []);
    public static MetadataValidationResult Failure(params string[] errors) => new(false, errors);
}

public static class MetadataValidator
{
    private static readonly Dictionary<string, string[]> RequiredFields = new()
    {
        ["MINE_EXTRACTION"] = ["gpsCoordinates", "mineOperatorIdentity", "mineralogicalCertificateRef"],
        ["CONCENTRATION"] = ["facilityName", "processDescription", "inputWeightKg", "outputWeightKg", "concentrationRatio"],
        ["TRADING_TRANSFER"] = ["sellerIdentity", "buyerIdentity", "transferDate", "contractReference"],
        ["LABORATORY_ASSAY"] = ["laboratoryName", "assayMethod", "tungstenContentPct", "assayCertificateRef"],
        ["PRIMARY_PROCESSING"] = ["smelterId", "processType", "inputWeightKg", "outputWeightKg"],
        ["EXPORT_SHIPMENT"] = ["originCountry", "destinationCountry", "transportMode", "exportPermitRef"],
    };

    public static MetadataValidationResult Validate(string eventType, JsonElement metadata)
    {
        if (!RequiredFields.TryGetValue(eventType, out var required))
            return MetadataValidationResult.Failure($"Unknown event type: {eventType}");

        var errors = new List<string>();
        foreach (var field in required)
        {
            if (!metadata.TryGetProperty(field, out var prop) ||
                prop.ValueKind == JsonValueKind.Null ||
                (prop.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(prop.GetString())))
            {
                errors.Add($"Missing required metadata field: {field}");
            }
        }

        return errors.Count == 0
            ? MetadataValidationResult.Success()
            : MetadataValidationResult.Failure([.. errors]);
    }
}
