using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GetGeneratedDocument
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string DocumentType,
        string DownloadUrl,
        string? ShareToken,
        DateTime? ShareExpiresAt,
        DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var doc = await db.GeneratedDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == query.Id && d.TenantId == user.TenantId, ct);
            if (doc is null)
                return Result<Response>.Failure("Generated document not found");

            return Result<Response>.Success(new Response(
                doc.Id, doc.BatchId, doc.DocumentType,
                storage.GetDownloadUrl(doc.StorageKey),
                doc.ShareToken, doc.ShareExpiresAt, doc.GeneratedAt));
        }
    }
}
