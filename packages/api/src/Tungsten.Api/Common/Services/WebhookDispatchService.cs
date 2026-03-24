using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Services;

public interface IWebhookDispatchService
{
    Task DispatchAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default);
}

public class WebhookDispatchService(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<WebhookDispatchService> logger) : IWebhookDispatchService
{
    public async Task DispatchAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default)
    {
        var endpoints = await db.WebhookEndpoints.AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .ToListAsync(ct);

        foreach (var endpoint in endpoints)
        {
            if (!endpoint.Events.Split(',').Contains(eventType) && !endpoint.Events.Contains("*"))
                continue;

            try
            {
                var body = JsonSerializer.Serialize(new
                {
                    @event = eventType,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    data = payload,
                });

                var signature = ComputeSignature(body, endpoint.Secret);
                var client = httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
                request.Headers.Add("X-Webhook-Signature", signature);
                request.Headers.Add("X-Webhook-Event", eventType);

                var response = await client.SendAsync(request, ct);
                logger.LogInformation("Webhook {Event} to {Url}: {Status}", eventType, endpoint.Url, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Webhook {Event} to {Url} failed", eventType, endpoint.Url);
            }
        }
    }

    private static string ComputeSignature(string body, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, bodyBytes);
        return Convert.ToHexStringLower(hash);
    }
}
