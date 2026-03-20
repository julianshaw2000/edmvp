using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Documents;

public static class GetDocument
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

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
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var doc = await db.Documents.AsNoTracking()
                .Where(d => d.Id == query.Id && d.TenantId == user.TenantId)
                .FirstOrDefaultAsync(ct);

            if (doc is null)
                return Result<Response>.Failure("Document not found");

            return Result<Response>.Success(new Response(
                doc.Id, doc.FileName, doc.FileSizeBytes, doc.ContentType,
                doc.Sha256Hash, doc.DocumentType,
                storage.GetDownloadUrl(doc.StorageKey), doc.CreatedAt));
        }
    }
}
