using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Platform;

public static class DeleteTenant
{
    public record Command(Guid TenantId) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "DeleteTenant";
        public string EntityType => "Tenant";
    }

    public record Response(string TenantName, int UsersRemoved);

    public class Handler(AppDbContext db, UserManager<AppIdentityUser> userManager)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var tenant = await db.Tenants
                .FirstOrDefaultAsync(t => t.Id == cmd.TenantId, ct);

            if (tenant is null)
                return Result<Response>.Failure("Tenant not found");

            // Get all users for this tenant
            var users = await db.Users
                .Where(u => u.TenantId == cmd.TenantId)
                .ToListAsync(ct);

            // Delete Identity accounts for each user
            foreach (var user in users)
            {
                if (!user.IdentityUserId.StartsWith("pending|"))
                {
                    var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
                    if (identityUser is not null)
                        await userManager.DeleteAsync(identityUser);
                }
            }

            // Delete all tenant data in order (respecting FK constraints)
            await db.Notifications.Where(n => n.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.AuditLogs.Where(a => a.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.ComplianceChecks.Where(c => c.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.Documents.Where(d => d.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.GeneratedDocuments.Where(g => g.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.CustodyEvents.Where(e => e.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.ApiKeys.Where(k => k.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.WebhookEndpoints.Where(w => w.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.Jobs.Where(j => j.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.Batches.Where(b => b.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            await db.Users.Where(u => u.TenantId == cmd.TenantId).ExecuteDeleteAsync(ct);
            db.Tenants.Remove(tenant);

            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(tenant.Name, users.Count));
        }
    }
}
