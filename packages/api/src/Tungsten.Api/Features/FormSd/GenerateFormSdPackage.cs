using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.FormSd.Templates;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateFormSdPackage
{
    public record Command(int ReportingYear) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "GenerateFormSdPackage";
        public string EntityType => "FormSdPackage";
    }

    public record Response(Guid Id, string DownloadUrl, DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage, IMediator mediator)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking().Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null) return Result<Response>.Failure("User not found");

            var tenantId = user.TenantId;
            var yearStart = new DateTime(cmd.ReportingYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = new DateTime(cmd.ReportingYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var batches = await db.Batches.AsNoTracking()
                .Where(b => b.TenantId == tenantId && b.CreatedAt >= yearStart && b.CreatedAt < yearEnd)
                .ToListAsync(ct);
            if (batches.Count == 0) return Result<Response>.Failure("No batches found for reporting year");

            var applicability = new List<BatchApplicability>();
            var supplyChains = new List<BatchSupplyChain>();
            var dueDiligence = new List<BatchDueDiligence>();
            var riskAssessments = new List<BatchRiskAssessment>();

            foreach (var batch in batches)
            {
                var app = await ApplicabilityEngine.EvaluateAsync(db, batch.Id, tenantId, ct);
                applicability.Add(new BatchApplicability(batch.BatchNumber, batch.MineralType, batch.OriginCountry, app.Status, app.Reasoning?.ToString() ?? ""));

                var sc = await mediator.Send(new GenerateSupplyChainDescription.Query(batch.Id), ct);
                if (sc.IsSuccess) supplyChains.Add(new BatchSupplyChain(batch.BatchNumber, sc.Value.NarrativeText, sc.Value.Chain.Count, sc.Value.Gaps.Count));

                var dd = await mediator.Send(new GenerateDueDiligenceSummary.Query(batch.Id), ct);
                if (dd.IsSuccess) dueDiligence.Add(new BatchDueDiligence(batch.BatchNumber, dd.Value.RiskFlags.Count, dd.Value.OecdDdgVersion, dd.Value.SummaryText, dd.Value.Smelters.Select(s => s.SmelterName).ToList()));

                var ra = await mediator.Send(new GenerateRiskAssessment.Query(batch.Id), ct);
                if (ra.IsSuccess) riskAssessments.Add(new BatchRiskAssessment(batch.BatchNumber, ra.Value.OverallRating, ra.Value.Categories.Select(c => new RiskCategoryItem(c.Category, c.Rating, c.Detail)).ToList()));
            }

            var packageData = new FormSdPackageData(user.Tenant.Name, cmd.ReportingYear, DateTime.UtcNow, user.DisplayName, "1.0.0", "2.0.0", applicability, supplyChains, dueDiligence, riskAssessments);

            var template = new FormSdPackageTemplate(packageData);
            using var pdfStream = new MemoryStream();
            template.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            var hash = Convert.ToHexStringLower(SHA256.HashData(pdfStream.ToArray()));
            pdfStream.Position = 0;

            var storageKey = $"{tenantId}/form-sd/{cmd.ReportingYear}/{Guid.NewGuid()}.pdf";
            await storage.UploadAsync(storageKey, pdfStream, "application/pdf", ct);

            var package = new FormSdPackageEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, ReportingYear = cmd.ReportingYear,
                StorageKey = storageKey, Sha256Hash = hash, RuleSetVersion = "1.0.0",
                PlatformVersion = "2.0.0", GeneratedBy = user.Id,
                SourceJson = JsonSerializer.Serialize(packageData), GeneratedAt = DateTime.UtcNow,
            };
            db.FormSdPackages.Add(package);

            var cycle = await db.FormSdFilingCycles
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ReportingYear == cmd.ReportingYear, ct);
            if (cycle is not null && cycle.Status is "NOT_STARTED" or "IN_PROGRESS")
            {
                cycle.Status = "PACKAGE_READY";
                cycle.UpdatedAt = DateTime.UtcNow;
            }

            db.Notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id,
                Type = "DOCUMENT_GENERATED", Title = "Form SD Support Package ready",
                Message = $"Form SD Support Package for {cmd.ReportingYear} is ready.",
                ReferenceId = package.Id, CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);
            return Result<Response>.Success(new Response(package.Id, storage.GetDownloadUrl(storageKey), package.GeneratedAt));
        }
    }
}
