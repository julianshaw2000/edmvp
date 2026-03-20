using System.Security.Cryptography;
using System.Text;

namespace Tungsten.Api.Common.Services;

public static class IdempotencyKeyService
{
    public static string GenerateKey(Guid batchId, string eventType, string eventDate, string location, string actorName)
    {
        var input = $"{batchId}|{eventType}|{eventDate}|{location}|{actorName}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }
}
