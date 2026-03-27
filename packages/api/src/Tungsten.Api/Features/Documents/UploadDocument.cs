using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Documents;

public static class UploadDocument
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
        "image/gif",
    ];

    private static readonly HashSet<string> ValidDocumentTypes =
    [
        "CERTIFICATE_OF_ORIGIN", "ASSAY_REPORT", "TRANSPORT_DOCUMENT",
        "SMELTER_CERTIFICATE", "MINERALOGICAL_CERTIFICATE", "EXPORT_PERMIT", "OTHER"
    ];

    public record Command(
        Guid EventId,
        string FileName,
        string ContentType,
        Stream FileStream,
        long FileSizeBytes,
        string DocumentType) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "UploadDocument";
        public string EntityType => "Document";
    }

    public record Response(
        Guid Id,
        string FileName,
        long FileSizeBytes,
        string ContentType,
        string Sha256Hash,
        string DocumentType,
        string DownloadUrl,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            // Validate file size
            if (cmd.FileSizeBytes > MaxFileSizeBytes)
                return Result<Response>.Failure($"File exceeds maximum size of 25MB");

            // Validate content type
            if (!AllowedContentTypes.Contains(cmd.ContentType))
                return Result<Response>.Failure($"Unsupported content type: {cmd.ContentType}. Allowed: PDF, JPEG, PNG, TIFF, GIF");

            // Validate document type
            if (!ValidDocumentTypes.Contains(cmd.DocumentType))
                return Result<Response>.Failure($"Invalid document type: {cmd.DocumentType}");

            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var evt = await db.CustodyEvents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == cmd.EventId && e.TenantId == user.TenantId, ct);
            if (evt is null)
                return Result<Response>.Failure("Event not found");

            // Compute SHA-256 hash of file content (FR-P031)
            cmd.FileStream.Position = 0;
            var hashBytes = await SHA256.HashDataAsync(cmd.FileStream, ct);
            var sha256Hash = Convert.ToHexStringLower(hashBytes);

            // Upload to storage
            cmd.FileStream.Position = 0;
            var storageKey = $"documents/{user.TenantId}/{evt.BatchId}/{Guid.NewGuid()}/{cmd.FileName}";
            await storage.UploadAsync(storageKey, cmd.FileStream, cmd.ContentType, ct);

            var doc = new DocumentEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                CustodyEventId = cmd.EventId,
                BatchId = evt.BatchId,
                FileName = cmd.FileName,
                StorageKey = storageKey,
                FileSizeBytes = cmd.FileSizeBytes,
                ContentType = cmd.ContentType,
                Sha256Hash = sha256Hash,
                DocumentType = cmd.DocumentType,
                UploadedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            db.Documents.Add(doc);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                doc.Id, doc.FileName, doc.FileSizeBytes, doc.ContentType,
                doc.Sha256Hash, doc.DocumentType,
                storage.GetDownloadUrl(storageKey), doc.CreatedAt));
        }
    }
}
