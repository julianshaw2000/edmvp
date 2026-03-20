using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Documents;

public static class ListBatchDocuments
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record DocumentItem(
        Guid Id,
        string FileName,
        long FileSizeBytes,
        string ContentType,
        string DocumentType,
        string DownloadUrl,
        DateTime CreatedAt);

    public record Response(IReadOnlyList<DocumentItem> Documents, int TotalCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            // Materialize first, then map download URLs (storage.GetDownloadUrl cannot be translated by EF Core)
            var rawDocs = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == query.BatchId && d.TenantId == user.TenantId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(ct);

            var docs = rawDocs.Select(d => new DocumentItem(
                d.Id, d.FileName, d.FileSizeBytes, d.ContentType,
                d.DocumentType, storage.GetDownloadUrl(d.StorageKey), d.CreatedAt))
                .ToList();

            return Result<Response>.Success(new Response(docs, docs.Count));
        }
    }
}
