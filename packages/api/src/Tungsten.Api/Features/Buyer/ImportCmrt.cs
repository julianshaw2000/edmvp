using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Buyer;

public static class ImportCmrt
{
    public record PreviewCommand(Stream FileStream, string FileName) : IRequest<Result<PreviewResponse>>;

    public record ConfirmCommand(
        string FileName,
        string DeclarationCompany,
        int? ReportingYear,
        List<SmelterMatchItem> Smelters) : IRequest<Result<ConfirmResponse>>;

    public record SmelterMatchItem(
        string MetalType,
        string? SmelterName,
        string? SmelterId,
        string? Country,
        string MatchStatus,
        string? MatchedSmelterId);

    public record PreviewResponse(
        string DeclarationCompany,
        int? ReportingYear,
        string? DeclarationScope,
        int TotalSmelters,
        int Matched,
        int Unmatched,
        int ErrorCount,
        List<SmelterPreviewItem> Smelters,
        List<string> Errors);

    public record SmelterPreviewItem(
        string MetalType,
        string? SmelterName,
        string? SmelterId,
        string? Country,
        string MatchStatus,
        string? MatchedSmelterId,
        string? MatchedSmelterName,
        string? ConformanceStatus,
        int RowNumber);

    public record ConfirmResponse(Guid ImportId, int Created, int Skipped);

    public class PreviewHandler(AppDbContext db)
        : IRequestHandler<PreviewCommand, Result<PreviewResponse>>
    {
        public async Task<Result<PreviewResponse>> Handle(PreviewCommand request, CancellationToken ct)
        {
            CmrtParseResult parseResult;
            try
            {
                parseResult = CmrtParserService.Parse(request.FileStream);
            }
            catch (Exception ex)
            {
                return Result<PreviewResponse>.Failure($"Failed to parse CMRT file: {ex.Message}");
            }

            var allRmapSmelters = await db.RmapSmelters.AsNoTracking().ToListAsync(ct);

            var previewItems = new List<SmelterPreviewItem>();
            var matched = 0;
            var unmatched = 0;

            foreach (var row in parseResult.Smelters)
            {
                RmapSmelterEntity? match = null;

                if (!string.IsNullOrWhiteSpace(row.SmelterId))
                    match = allRmapSmelters.FirstOrDefault(s =>
                        s.SmelterId.Equals(row.SmelterId, StringComparison.OrdinalIgnoreCase));

                if (match is null && !string.IsNullOrWhiteSpace(row.SmelterName) && !string.IsNullOrWhiteSpace(row.Country))
                    match = allRmapSmelters.FirstOrDefault(s =>
                        s.SmelterName.Equals(row.SmelterName, StringComparison.OrdinalIgnoreCase)
                        && s.Country.Equals(row.Country, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    matched++;
                    previewItems.Add(new SmelterPreviewItem(
                        row.MetalType, row.SmelterName, row.SmelterId, row.Country,
                        "matched", match.SmelterId, match.SmelterName, match.ConformanceStatus, row.RowNumber));
                }
                else
                {
                    unmatched++;
                    previewItems.Add(new SmelterPreviewItem(
                        row.MetalType, row.SmelterName, row.SmelterId, row.Country,
                        "unmatched", null, null, null, row.RowNumber));
                }
            }

            return Result<PreviewResponse>.Success(new PreviewResponse(
                parseResult.Declaration.CompanyName,
                parseResult.Declaration.ReportingYear,
                parseResult.Declaration.DeclarationScope,
                parseResult.Smelters.Count,
                matched, unmatched,
                parseResult.Errors.Count,
                previewItems,
                parseResult.Errors));
        }
    }

    public class ConfirmHandler(AppDbContext db, ICurrentUserService currentUser, ILogger<ConfirmHandler> logger)
        : IRequestHandler<ConfirmCommand, Result<ConfirmResponse>>
    {
        public async Task<Result<ConfirmResponse>> Handle(ConfirmCommand request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var userId = await currentUser.GetUserIdAsync(ct);

            var import = new CmrtImportEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FileName = request.FileName,
                DeclarationCompany = request.DeclarationCompany,
                ReportingYear = request.ReportingYear,
                RowsParsed = request.Smelters.Count,
                RowsMatched = request.Smelters.Count(s => s.MatchStatus == "matched"),
                RowsUnmatched = request.Smelters.Count(s => s.MatchStatus == "unmatched"),
                Errors = 0,
                ImportedBy = userId,
                ImportedAt = DateTime.UtcNow,
            };
            db.CmrtImports.Add(import);

            var created = 0;
            var skipped = 0;

            foreach (var smelter in request.Smelters.Where(s => s.MatchedSmelterId is not null))
            {
                var exists = await db.TenantSmelterAssociations.AnyAsync(a =>
                    a.TenantId == tenantId && a.SmelterId == smelter.MatchedSmelterId, ct);

                if (exists) { skipped++; continue; }

                db.TenantSmelterAssociations.Add(new TenantSmelterAssociationEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SmelterId = smelter.MatchedSmelterId!,
                    Source = "CMRT_IMPORT",
                    CmrtImportId = import.Id,
                    Status = "verified",
                    MetalType = smelter.MetalType,
                    CreatedAt = DateTime.UtcNow,
                });
                created++;
            }

            foreach (var smelter in request.Smelters.Where(s =>
                s.MatchStatus == "unmatched" && !string.IsNullOrWhiteSpace(s.SmelterId)))
            {
                var exists = await db.TenantSmelterAssociations.AnyAsync(a =>
                    a.TenantId == tenantId && a.SmelterId == smelter.SmelterId!, ct);

                if (!exists)
                {
                    db.TenantSmelterAssociations.Add(new TenantSmelterAssociationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        SmelterId = smelter.SmelterId!,
                        Source = "CMRT_IMPORT",
                        CmrtImportId = import.Id,
                        Status = "unverified",
                        MetalType = smelter.MetalType,
                        CreatedAt = DateTime.UtcNow,
                    });
                    created++;
                }
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation("CMRT import {ImportId}: {Created} associations created, {Skipped} skipped",
                import.Id, created, skipped);

            return Result<ConfirmResponse>.Success(new ConfirmResponse(import.Id, created, skipped));
        }
    }
}
