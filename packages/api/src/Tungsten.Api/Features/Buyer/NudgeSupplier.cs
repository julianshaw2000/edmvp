using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Buyer;

public static class NudgeSupplier
{
    public record Command(Guid SupplierId) : IRequest<Result>;

    public class Handler(
        AppDbContext db,
        ICurrentUserService currentUser,
        IEmailService emailService,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var supplier = await db.Users
                .FirstOrDefaultAsync(u => u.Id == request.SupplierId
                    && u.TenantId == tenantId
                    && u.Role == Roles.Supplier
                    && u.IsActive, ct);

            if (supplier is null)
                return Result.Failure("Supplier not found");

            if (supplier.LastNudgedAt.HasValue
                && supplier.LastNudgedAt.Value > DateTime.UtcNow.AddDays(-7))
                return Result.Failure($"Reminder already sent {(DateTime.UtcNow - supplier.LastNudgedAt.Value).Days} days ago. Please wait 7 days between reminders.");

            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            var companyName = tenant?.Name ?? "Your buyer";

            var (subject, htmlBody, textBody) = EmailTemplates.BuyerNudge(
                supplier.DisplayName, companyName);

            await emailService.SendAsync(supplier.Email, subject, htmlBody, textBody, ct);

            supplier.LastNudgedAt = DateTime.UtcNow;

            db.Notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = supplier.Id,
                Type = "BUYER_NUDGE",
                Title = "Update requested",
                Message = $"{companyName} is requesting an update on your supply chain data.",
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Nudge sent to supplier {SupplierId} by tenant {TenantId}",
                request.SupplierId, tenantId);

            return Result.Success();
        }
    }
}
