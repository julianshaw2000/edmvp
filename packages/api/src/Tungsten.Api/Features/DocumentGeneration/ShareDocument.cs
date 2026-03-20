using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class ShareDocument
{
    public record Command(Guid DocumentId) : IRequest<Result<Response>>;

    public record Response(string ShareUrl, DateTime ExpiresAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var doc = await db.GeneratedDocuments
                .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId && d.TenantId == user.TenantId, ct);
            if (doc is null)
                return Result<Response>.Failure("Generated document not found");

            // Generate URL-safe token
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var expiresAt = DateTime.UtcNow.AddDays(30);

            doc.ShareToken = token;
            doc.ShareExpiresAt = expiresAt;
            await db.SaveChangesAsync(ct);

            var baseUrl = config["App:BaseUrl"] ?? "https://tungsten.example.com";
            var shareUrl = $"{baseUrl}/api/shared/{token}";

            return Result<Response>.Success(new Response(shareUrl, expiresAt));
        }
    }
}
