using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class UsageCoaching
{
    public record Query : IRequest<Result<Response>>;

    public record CoachingSuggestion(
        Guid TenantId,
        string TenantName,
        string Category,
        string Suggestion,
        string SuggestedAction);

    public record Response(List<CoachingSuggestion> Suggestions);

    public class Handler(AppDbContext db, IAiService ai) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var cutoff14 = DateTime.UtcNow.AddDays(-14);

            var tenants = await db.Tenants.AsNoTracking()
                .Where(t => t.Status != "CANCELLED")
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Status,
                    t.MaxBatches,
                    BatchCount = db.Batches.Count(b => b.TenantId == t.Id),
                    HasPassport = db.GeneratedDocuments.Any(d => d.TenantId == t.Id && d.DocumentType == "MaterialPassport"),
                    LastEvent = db.CustodyEvents.Where(e => e.TenantId == t.Id).Select(e => (DateTime?)e.CreatedAt).Max(),
                })
                .ToListAsync(ct);

            var rawSuggestions = new List<(Guid TenantId, string TenantName, string Category, string Issue)>();

            foreach (var t in tenants)
            {
                var lastEventDate = t.LastEvent;
                var noRecentEvents = !lastEventDate.HasValue || lastEventDate.Value < cutoff14;

                if (noRecentEvents)
                    rawSuggestions.Add((t.Id, t.Name, "Engagement", $"Tenant '{t.Name}' has had no custody events in 14+ days."));

                if (!t.HasPassport && t.BatchCount > 0)
                    rawSuggestions.Add((t.Id, t.Name, "Feature Adoption", $"Tenant '{t.Name}' has {t.BatchCount} batch(es) but has never generated a Material Passport."));

                if (t.MaxBatches.HasValue && t.BatchCount >= t.MaxBatches.Value * 0.8)
                    rawSuggestions.Add((t.Id, t.Name, "Plan Limit", $"Tenant '{t.Name}' is at {t.BatchCount}/{t.MaxBatches} batches (80%+ of plan limit)."));
            }

            if (rawSuggestions.Count == 0)
                return Result<Response>.Success(new Response([]));

            var issueList = string.Join("\n", rawSuggestions.Select((s, i) => $"{i + 1}. [{s.Category}] {s.Issue}"));

            var systemPrompt = """
                You are a customer success coach for auditraks, a 3TG mineral supply chain compliance platform.
                For each issue listed, write a friendly, concise coaching suggestion (1-2 sentences) and a clear suggested action.
                Format your response as a numbered list matching the input. Each entry: "Suggestion: ... | Action: ..."
                Be encouraging, not alarming. Focus on value and outcomes.
                """;

            var aiResponse = await ai.GenerateAsync(systemPrompt, issueList, ct);

            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var suggestions = new List<CoachingSuggestion>();

            for (var i = 0; i < rawSuggestions.Count; i++)
            {
                var raw = rawSuggestions[i];
                var suggestion = "Review platform activity and reach out to the team.";
                var action = "Contact tenant for check-in.";

                if (i < lines.Length)
                {
                    var line = lines[i].Trim().TrimStart("0123456789. ".ToCharArray());
                    var parts = line.Split('|', 2);
                    if (parts.Length == 2)
                    {
                        suggestion = parts[0].Replace("Suggestion:", "").Trim();
                        action = parts[1].Replace("Action:", "").Trim();
                    }
                    else if (parts.Length == 1 && parts[0].Length > 10)
                    {
                        suggestion = parts[0].Replace("Suggestion:", "").Trim();
                    }
                }

                suggestions.Add(new CoachingSuggestion(raw.TenantId, raw.TenantName, raw.Category, suggestion, action));
            }

            return Result<Response>.Success(new Response(suggestions));
        }
    }
}
