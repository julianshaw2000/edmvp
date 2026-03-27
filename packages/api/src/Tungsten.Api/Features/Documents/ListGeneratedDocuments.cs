using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Documents;

public static class ListGeneratedDocuments
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record GeneratedDocumentItem(
        Guid Id,
        Guid BatchId,
        string DocumentType,
        string DownloadUrl,
        string? ShareToken,
        DateTime? ShareExpiresAt,
        DateTime GeneratedAt);

    public record Response(IReadOnlyList<GeneratedDocumentItem> Documents, int TotalCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var rawDocs = await db.GeneratedDocuments.AsNoTracking()
                .Where(d => d.BatchId == query.BatchId && d.TenantId == user.TenantId)
                .OrderByDescending(d => d.GeneratedAt)
                .ToListAsync(ct);

            var docs = rawDocs.Select(d => new GeneratedDocumentItem(
                d.Id, d.BatchId, d.DocumentType,
                storage.GetDownloadUrl(d.StorageKey),
                d.ShareToken, d.ShareExpiresAt, d.GeneratedAt))
                .ToList();

            return Result<Response>.Success(new Response(docs, docs.Count));
        }
    }
}
