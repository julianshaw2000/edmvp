using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class NaturalLanguageQuery
{
    public record Command(string Question) : IRequest<Result<Response>>;

    public record QueryFilters(
        string? EntityType,
        string? Status,
        string? ComplianceStatus,
        string? OriginCountry,
        string? MineralType,
        DateTime? DateFrom,
        DateTime? DateTo);

    public record QueryResult(string EntityType, int TotalCount, List<object> Items);
    public record Response(string Question, QueryFilters ParsedFilters, QueryResult Results);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public class Handler(AppDbContext db, IAiService ai) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var schemaContext = """
                Database schema for auditraks compliance platform:

                Batches table: Id, TenantId, BatchNumber, MineralType (TUNGSTEN/TIN/TANTALUM/GOLD), OriginCountry, OriginMine, WeightKg, Status (ACTIVE/CLOSED/CANCELLED), ComplianceStatus (PENDING/COMPLIANT/FLAGGED/REVIEW_REQUIRED), CreatedAt, UpdatedAt

                CustodyEvents table: Id, BatchId, TenantId, EventType (MINE_EXTRACTION/TRANSPORT/SMELTING/PROCESSING/EXPORT/IMPORT/SALE/CERTIFICATION), EventDate, Location, ActorName, SmelterId, Description, CreatedAt

                Users table: Id, TenantId, Email, FirstName, LastName, Role (SUPPLIER/BUYER/TENANT_ADMIN), IsActive, CreatedAt

                Available filters to extract from the question:
                - entityType: "batch", "event", or "user"
                - status: batch status or user IsActive (true/false as string)
                - complianceStatus: PENDING, COMPLIANT, FLAGGED, REVIEW_REQUIRED
                - originCountry: country name
                - mineralType: TUNGSTEN, TIN, TANTALUM, GOLD
                - dateFrom: ISO date string (YYYY-MM-DD)
                - dateTo: ISO date string (YYYY-MM-DD)

                Return ONLY a JSON object with these exact fields (null for unspecified):
                {"entityType":"batch","status":null,"complianceStatus":"FLAGGED","originCountry":null,"mineralType":"TUNGSTEN","dateFrom":null,"dateTo":null}
                """;

            var aiResponse = await ai.GenerateAsync(schemaContext, $"Extract filters from this question: {cmd.Question}", ct);

            // Parse AI response — extract JSON block
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            QueryFilters filters;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                try
                {
                    var json = aiResponse[jsonStart..(jsonEnd + 1)];
                    var parsed = JsonSerializer.Deserialize<JsonElement>(json);
                    filters = new QueryFilters(
                        EntityType: parsed.TryGetProperty("entityType", out var et) ? et.GetString() : "batch",
                        Status: parsed.TryGetProperty("status", out var st) ? st.GetString() : null,
                        ComplianceStatus: parsed.TryGetProperty("complianceStatus", out var cs) ? cs.GetString() : null,
                        OriginCountry: parsed.TryGetProperty("originCountry", out var oc) ? oc.GetString() : null,
                        MineralType: parsed.TryGetProperty("mineralType", out var mt) ? mt.GetString() : null,
                        DateFrom: parsed.TryGetProperty("dateFrom", out var df) && df.GetString() != null
                            ? DateTime.TryParse(df.GetString(), out var dfParsed) ? dfParsed : null
                            : null,
                        DateTo: parsed.TryGetProperty("dateTo", out var dt) && dt.GetString() != null
                            ? DateTime.TryParse(dt.GetString(), out var dtParsed) ? dtParsed : null
                            : null
                    );
                }
                catch
                {
                    filters = new QueryFilters("batch", null, null, null, null, null, null);
                }
            }
            else
            {
                filters = new QueryFilters("batch", null, null, null, null, null, null);
            }

            var entityType = filters.EntityType?.ToLowerInvariant() ?? "batch";
            QueryResult queryResult;

            if (entityType == "event")
            {
                var eventsQuery = db.CustodyEvents.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(filters.Status))
                    eventsQuery = eventsQuery.Where(e => e.EventType == filters.Status);
                if (filters.DateFrom.HasValue)
                    eventsQuery = eventsQuery.Where(e => e.EventDate >= filters.DateFrom.Value);
                if (filters.DateTo.HasValue)
                    eventsQuery = eventsQuery.Where(e => e.EventDate <= filters.DateTo.Value);

                var events = await eventsQuery
                    .OrderByDescending(e => e.EventDate)
                    .Take(50)
                    .Select(e => new { e.Id, e.EventType, e.EventDate, e.Location, e.ActorName, e.Description, BatchId = e.BatchId })
                    .ToListAsync(ct);

                queryResult = new QueryResult("event", events.Count, events.Cast<object>().ToList());
            }
            else if (entityType == "user")
            {
                var usersQuery = db.Users.AsNoTracking().AsQueryable();
                if (filters.Status == "true")
                    usersQuery = usersQuery.Where(u => u.IsActive);
                else if (filters.Status == "false")
                    usersQuery = usersQuery.Where(u => !u.IsActive);
                if (filters.DateFrom.HasValue)
                    usersQuery = usersQuery.Where(u => u.CreatedAt >= filters.DateFrom.Value);
                if (filters.DateTo.HasValue)
                    usersQuery = usersQuery.Where(u => u.CreatedAt <= filters.DateTo.Value);

                var users = await usersQuery
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(50)
                    .Select(u => new { u.Id, u.Email, u.DisplayName, u.Role, u.IsActive, u.CreatedAt, u.TenantId })
                    .ToListAsync(ct);

                queryResult = new QueryResult("user", users.Count, users.Cast<object>().ToList());
            }
            else
            {
                var batchesQuery = db.Batches.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(filters.Status))
                    batchesQuery = batchesQuery.Where(b => b.Status == filters.Status);
                if (!string.IsNullOrEmpty(filters.ComplianceStatus))
                    batchesQuery = batchesQuery.Where(b => b.ComplianceStatus == filters.ComplianceStatus);
                if (!string.IsNullOrEmpty(filters.OriginCountry))
                    batchesQuery = batchesQuery.Where(b => b.OriginCountry == filters.OriginCountry);
                if (!string.IsNullOrEmpty(filters.MineralType))
                    batchesQuery = batchesQuery.Where(b => b.MineralType == filters.MineralType);
                if (filters.DateFrom.HasValue)
                    batchesQuery = batchesQuery.Where(b => b.CreatedAt >= filters.DateFrom.Value);
                if (filters.DateTo.HasValue)
                    batchesQuery = batchesQuery.Where(b => b.CreatedAt <= filters.DateTo.Value);

                var batches = await batchesQuery
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(50)
                    .Select(b => new { b.Id, b.BatchNumber, b.MineralType, b.OriginCountry, b.OriginMine, b.WeightKg, b.Status, b.ComplianceStatus, b.CreatedAt })
                    .ToListAsync(ct);

                queryResult = new QueryResult("batch", batches.Count, batches.Cast<object>().ToList());
            }

            return Result<Response>.Success(new Response(cmd.Question, filters, queryResult));
        }
    }
}
