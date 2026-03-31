using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class ShareDocumentEmail
{
    public record Command(Guid DocumentId, string RecipientEmail, string? Message)
        : IRequest<Result<Response>>;

    public record Response(string ShareUrl);

    public class Handler(
        AppDbContext db,
        IMediator mediator,
        ICurrentUserService currentUser,
        IEmailService emailService,
        IConfiguration config,
        ILogger<Handler> logger) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var doc = await db.GeneratedDocuments.AsNoTracking()
                .Include(d => d.Batch)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

            if (doc is null)
                return Result<Response>.Failure("Document not found");

            var tenantId = await currentUser.GetTenantIdAsync(ct);
            if (doc.TenantId != tenantId)
                return Result<Response>.Failure("Document not found");

            string shareUrl;
            if (!string.IsNullOrEmpty(doc.ShareToken) && doc.ShareExpiresAt > DateTime.UtcNow)
            {
                var baseUrl = config["App:BaseUrl"] ?? "https://auditraks.com";
                shareUrl = $"{baseUrl}/api/shared/{doc.ShareToken}";
            }
            else
            {
                var shareResult = await mediator.Send(new ShareDocument.Command(request.DocumentId), ct);
                if (!shareResult.IsSuccess)
                    return Result<Response>.Failure(shareResult.Error!);
                shareUrl = shareResult.Value.ShareUrl;
            }

            var userId = await currentUser.GetUserIdAsync(ct);
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            var senderName = user?.DisplayName ?? "A supplier";

            var (subject, htmlBody, textBody) = EmailTemplates.PassportShared(
                doc.Batch.BatchNumber, senderName, shareUrl, request.Message);

            await emailService.SendAsync(request.RecipientEmail, subject, htmlBody, textBody, ct);

            logger.LogInformation("Passport shared via email to {Recipient} for batch {BatchId}",
                request.RecipientEmail, doc.BatchId);

            return Result<Response>.Success(new Response(shareUrl));
        }
    }
}
