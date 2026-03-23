using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Admin;

public static class ListAuditLogs
{
    public record Query(
        int Page,
        int PageSize,
        Guid? UserId,
        string? Action,
        string? EntityType,
        DateTime? From,
        DateTime? To,
        Guid? TenantId) : IRequest<Result<PagedResponse<AuditLogDto>>>;

    public record AuditLogDto(
        Guid Id,
        Guid UserId,
        string UserDisplayName,
        string Action,
        string EntityType,
        Guid? EntityId,
        JsonElement? Payload,
        string Result,
        string? FailureReason,
        string? IpAddress,
        DateTime Timestamp);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<PagedResponse<AuditLogDto>>>
    {
        public async Task<Result<PagedResponse<AuditLogDto>>> Handle(Query query, CancellationToken ct)
        {
            var callerRole = await currentUser.GetRoleAsync(ct);
            var q = db.AuditLogs.AsNoTracking();

            if (callerRole == Roles.Admin)
            {
                // PLATFORM_ADMIN: see all tenants, optionally filter by one
                if (query.TenantId.HasValue)
                    q = q.Where(a => a.TenantId == query.TenantId.Value);
            }
            else
            {
                // TENANT_ADMIN: scoped to own tenant only
                var tenantId = await currentUser.GetTenantIdAsync(ct);
                q = q.Where(a => a.TenantId == tenantId);
            }

            if (query.UserId.HasValue)
                q = q.Where(a => a.UserId == query.UserId.Value);
            if (!string.IsNullOrEmpty(query.Action))
                q = q.Where(a => a.Action == query.Action);
            if (!string.IsNullOrEmpty(query.EntityType))
                q = q.Where(a => a.EntityType == query.EntityType);
            if (query.From.HasValue)
                q = q.Where(a => a.Timestamp >= query.From.Value);
            if (query.To.HasValue)
                q = q.Where(a => a.Timestamp <= query.To.Value);

            var totalCount = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(a => a.Timestamp)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new AuditLogDto(
                    a.Id, a.UserId, u.DisplayName, a.Action, a.EntityType,
                    a.EntityId, a.Payload, a.Result, a.FailureReason,
                    a.IpAddress, a.Timestamp))
                .ToListAsync(ct);

            return Result<PagedResponse<AuditLogDto>>.Success(
                new PagedResponse<AuditLogDto>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
