using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tungsten.Api.Common.Services;

public static class HashService
{
    /// <summary>
    /// Normalizes a date string to UTC ISO 8601 format for consistent hashing.
    /// Both creation and verification paths MUST use this to avoid format mismatches.
    /// </summary>
    public static string NormalizeDate(string dateString) =>
        DateTime.Parse(dateString).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

    public static string NormalizeDate(DateTime date) =>
        date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

    public static string ComputeEventHash(
        string eventType,
        string eventDate,
        Guid batchId,
        string location,
        string actorName,
        string? smelterId,
        string description,
        string metadata,
        string? previousEventHash)
    {
        // Use SortedDictionary for guaranteed stable key ordering
        var fields = new SortedDictionary<string, string>
        {
            ["actor_name"] = actorName,
            ["batch_id"] = batchId.ToString(),
            ["description"] = description,
            ["event_date"] = eventDate, // caller must pre-normalize via NormalizeDate
            ["event_type"] = eventType,
            ["location"] = location,
            ["metadata"] = metadata,
            ["previous_event_hash"] = previousEventHash ?? "",
            ["smelter_id"] = smelterId ?? "",
        };

        var canonical = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = false });
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(hashBytes);
    }
}
