using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Admin;

public static class ExportAuditLogs
{
    public record Query(Guid? UserId, string? Action, string? EntityType, DateTime? From, DateTime? To)
        : IRequest<Result<byte[]>>;

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<byte[]>>
    {
        public async Task<Result<byte[]>> Handle(Query query, CancellationToken ct)
        {
            var callerRole = await currentUser.GetRoleAsync(ct);
            var q = db.AuditLogs.AsNoTracking();

            if (callerRole == Roles.Admin)
            {
                // PLATFORM_ADMIN sees all
            }
            else
            {
                var tenantId = await currentUser.GetTenantIdAsync(ct);
                q = q.Where(a => a.TenantId == tenantId);
            }

            if (query.UserId.HasValue) q = q.Where(a => a.UserId == query.UserId.Value);
            if (!string.IsNullOrEmpty(query.Action)) q = q.Where(a => a.Action == query.Action);
            if (!string.IsNullOrEmpty(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
            if (query.From.HasValue) q = q.Where(a => a.Timestamp >= query.From.Value);
            if (query.To.HasValue) q = q.Where(a => a.Timestamp <= query.To.Value);

            var items = await q
                .OrderByDescending(a => a.Timestamp)
                .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { a.Timestamp, User = u.DisplayName, a.Action, a.EntityType, a.EntityId, a.Result, a.FailureReason, a.IpAddress })
                .ToListAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,User,Action,EntityType,EntityId,Result,FailureReason,IpAddress");
            foreach (var item in items)
            {
                sb.AppendLine($"\"{item.Timestamp:O}\",\"{item.User}\",\"{item.Action}\",\"{item.EntityType}\",\"{item.EntityId}\",\"{item.Result}\",\"{item.FailureReason ?? ""}\",\"{item.IpAddress ?? ""}\"");
            }

            return Result<byte[]>.Success(Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }
}
