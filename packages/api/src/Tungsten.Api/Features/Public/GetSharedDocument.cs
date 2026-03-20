using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Public;

public static class GetSharedDocument
{
    public record Query(string Token) : IRequest<Result<Response>>;

    public record Response(string DownloadUrl, string DocumentType, DateTime GeneratedAt);

    public class Handler(AppDbContext db, IFileStorageService storage) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var doc = await db.GeneratedDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.ShareToken == query.Token, ct);

            if (doc is null)
                return Result<Response>.Failure("Shared document not found");

            if (doc.ShareExpiresAt.HasValue && doc.ShareExpiresAt.Value < DateTime.UtcNow)
                return Result<Response>.Failure("Share link has expired");

            return Result<Response>.Success(new Response(
                storage.GetDownloadUrl(doc.StorageKey),
                doc.DocumentType, doc.GeneratedAt));
        }
    }
}
