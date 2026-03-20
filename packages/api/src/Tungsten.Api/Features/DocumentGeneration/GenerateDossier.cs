using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.DocumentGeneration.Templates;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GenerateDossier
{
    public record Command(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(Guid Id, string DownloadUrl, DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new DossierEventData(
                    e.EventType, e.EventDate, e.Location,
                    e.ActorName, e.SmelterId, e.Description,
                    e.IsCorrection, e.CorrectsEventId,
                    e.Sha256Hash, e.PreviousEventHash))
                .ToListAsync(ct);

            // Materialize first to avoid EF Core translation issues with JsonElement.GetRawText()
            var rawChecks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == cmd.BatchId)
                .Join(db.CustodyEvents, c => c.CustodyEventId, e => e.Id, (c, e) => new { c, e })
                .ToListAsync(ct);

            var checks = rawChecks.Select(x => new DossierComplianceData(
                x.e.EventType, x.c.Framework, x.c.Status,
                x.c.Details.HasValue ? x.c.Details.Value.GetRawText() : "{}",
                x.c.CheckedAt)).ToList();

            // Materialize docs join first (EF Core may not translate all joins with projections)
            var rawDocs = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == cmd.BatchId)
                .Join(db.Users, d => d.UploadedBy, u => u.Id, (d, u) => new { d, u })
                .ToListAsync(ct);

            var docs = rawDocs.Select(x => new DossierDocumentData(
                x.d.FileName, x.d.DocumentType, x.d.FileSizeBytes,
                x.u.DisplayName, x.d.CreatedAt, x.d.Sha256Hash)).ToList();

            // Verify hash chain integrity
            var allEvents = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            var hashChainIntact = VerifyChain(allEvents);

            var dossierData = new DossierData(
                batch.BatchNumber, user.Tenant.Name, batch.MineralType,
                batch.OriginCountry, batch.OriginMine, batch.WeightKg,
                batch.Status, batch.ComplianceStatus,
                events, checks, docs, hashChainIntact, user.DisplayName, DateTime.UtcNow);

            var template = new DossierTemplate(dossierData);
            using var pdfStream = new MemoryStream();
            template.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            var storageKey = $"dossiers/{user.TenantId}/{cmd.BatchId}/{Guid.NewGuid()}.pdf";
            await storage.UploadAsync(storageKey, pdfStream, "application/pdf", ct);

            var genDoc = new GeneratedDocumentEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                DocumentType = "AUDIT_DOSSIER",
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
                Title = "Audit Dossier generated",
                Message = $"Audit Dossier for batch {batch.BatchNumber} is ready for download.",
                ReferenceId = genDoc.Id,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                genDoc.Id, storage.GetDownloadUrl(storageKey), genDoc.GeneratedAt));
        }

        private static bool VerifyChain(List<CustodyEventEntity> events)
        {
            string? previousHash = null;
            foreach (var evt in events)
            {
                if (evt.PreviousEventHash != previousHash)
                    return false;
                previousHash = evt.Sha256Hash;
            }
            return true;
        }
    }
}
