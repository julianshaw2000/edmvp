using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GenerateDpp
{
    public record Command(Guid BatchId) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "GenerateDpp";
        public string EntityType => "GeneratedDocument";
    }

    public record Response(Guid Id, string DownloadUrl, DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new { e.EventType, e.EventDate, e.Location, e.ActorName, e.Sha256Hash })
                .ToListAsync(ct);

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == cmd.BatchId)
                .Select(c => new { c.Framework, c.Status, c.CheckedAt })
                .ToListAsync(ct);

            // Verify hash chain
            var allEvents = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);
            var hashChainIntact = VerifyChain(allEvents);

            var baseUrl = config["App:BaseUrl"] ?? "https://auditraks.com";

            // Build DPP JSON-LD
            var dpp = new
            {
                context = new
                {
                    vocab = "https://schema.org/",
                    dpp = "https://auditraks.com/schemas/dpp/v1/",
                },
                type = "dpp:DigitalProductPassport",
                id = $"{baseUrl}/verify/{batch.Id}",
                passportVersion = "1.0",
                issuedDate = DateTime.UtcNow.ToString("O"),
                issuer = new
                {
                    type = "Organization",
                    name = user.Tenant.Name,
                    url = baseUrl,
                },
                product = new
                {
                    type = "Product",
                    identifier = batch.BatchNumber,
                    name = batch.MineralType,
                    weight = new { type = "QuantitativeValue", value = batch.WeightKg, unitCode = "KGM" },
                    countryOfOrigin = batch.OriginCountry,
                    productionFacility = batch.OriginMine,
                },
                supplyChain = new
                {
                    type = "dpp:CustodyChain",
                    totalEvents = events.Count,
                    integrityMethod = "SHA-256 hash chain",
                    hashChainIntact,
                    events = events.Select(e => new
                    {
                        type = "dpp:CustodyEvent",
                        eventType = e.EventType,
                        date = e.EventDate.ToString("O"),
                        location = e.Location,
                        actor = e.ActorName,
                    }),
                },
                compliance = new
                {
                    type = "dpp:ComplianceStatus",
                    overallStatus = batch.ComplianceStatus,
                    frameworks = checks
                        .GroupBy(c => c.Framework)
                        .Select(g => new
                        {
                            name = g.Key,
                            status = g.OrderByDescending(c => c.CheckedAt).First().Status,
                            checkedAt = g.Max(c => c.CheckedAt).ToString("O"),
                        }),
                },
                verification = new
                {
                    type = "dpp:VerificationInfo",
                    qrCodeUrl = $"{baseUrl}/verify/{batch.Id}",
                    hashChainIntact,
                },
            };

            // Serialize with JSON-LD conventions
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dpp, jsonOptions);

            // Store
            var storageKey = $"dpp/{user.TenantId}/{cmd.BatchId}/{Guid.NewGuid()}.json";
            using var stream = new MemoryStream(jsonBytes);
            await storage.UploadAsync(storageKey, stream, "application/ld+json", ct);

            var genDoc = new GeneratedDocumentEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                DocumentType = "DIGITAL_PRODUCT_PASSPORT",
                StorageKey = storageKey,
                GeneratedBy = user.Id,
                GeneratedAt = DateTime.UtcNow,
            };

            db.GeneratedDocuments.Add(genDoc);

            db.Notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = user.Id,
                Type = "DOCUMENT_GENERATED",
                Title = "Digital Product Passport generated",
                Message = $"DPP for batch {batch.BatchNumber} is ready for download and sharing.",
                ReferenceId = genDoc.Id,
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);

            var downloadUrl = storage.GetDownloadUrl(storageKey);
            return Result<Response>.Success(new Response(genDoc.Id, downloadUrl, genDoc.GeneratedAt));
        }

        private static bool VerifyChain(List<CustodyEventEntity> events)
        {
            if (events.Count <= 1) return true;
            for (var i = 1; i < events.Count; i++)
            {
                if (events[i].PreviousEventHash != events[i - 1].Sha256Hash)
                    return false;
            }
            return true;
        }
    }
}
