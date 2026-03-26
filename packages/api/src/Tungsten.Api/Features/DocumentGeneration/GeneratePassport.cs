using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.DocumentGeneration.Templates;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GeneratePassport
{
    public record Command(Guid BatchId) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "GeneratePassport";
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
                .FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new PassportEventData(
                    e.EventType, e.EventDate, e.Location,
                    e.ActorName, e.IsCorrection, e.Sha256Hash))
                .ToListAsync(ct);

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == cmd.BatchId)
                .Join(db.CustodyEvents, c => c.CustodyEventId, e => e.Id, (c, e) => new { c, e })
                .Select(x => new PassportComplianceData(
                    x.e.EventType, x.c.Framework, x.c.Status, x.c.CheckedAt))
                .ToListAsync(ct);

            var docs = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == cmd.BatchId)
                .Select(d => new PassportDocumentData(d.FileName, d.DocumentType, d.CreatedAt))
                .ToListAsync(ct);

            // Verify hash chain integrity
            var allEvents = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            var hashChainIntact = VerifyChain(allEvents);

            var baseUrl = config["App:BaseUrl"] ?? "https://accutrac.org";
            var verificationUrl = $"{baseUrl}/verify/{batch.BatchNumber}";

            var passportData = new PassportData(
                batch.BatchNumber, user.Tenant.Name, batch.MineralType,
                batch.OriginCountry, batch.OriginMine, batch.WeightKg,
                batch.Status, batch.ComplianceStatus, verificationUrl,
                events, checks, docs, hashChainIntact, user.DisplayName, DateTime.UtcNow);

            // Generate PDF
            var template = new PassportTemplate(passportData);
            using var pdfStream = new MemoryStream();
            template.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            // Store
            var storageKey = $"passports/{user.TenantId}/{cmd.BatchId}/{Guid.NewGuid()}.pdf";
            await storage.UploadAsync(storageKey, pdfStream, "application/pdf", ct);

            var genDoc = new GeneratedDocumentEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                DocumentType = "MATERIAL_PASSPORT",
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
                Title = "Material Passport generated",
                Message = $"Material Passport for batch {batch.BatchNumber} is ready for download.",
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
