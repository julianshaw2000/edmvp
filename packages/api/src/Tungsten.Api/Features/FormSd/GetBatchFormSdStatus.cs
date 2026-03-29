using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GetBatchFormSdStatus
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;
    public record Response(string ApplicabilityStatus, string? RuleSetVersion, string? Reasoning, DateTime? AssessedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var assessment = await db.FormSdAssessments.AsNoTracking()
                .Where(a => a.BatchId == query.BatchId && a.TenantId == tenantId && a.SupersedesId == null)
                .OrderByDescending(a => a.AssessedAt).FirstOrDefaultAsync(ct);

            return assessment is null
                ? Result<Response>.Success(new Response("NOT_ASSESSED", null, null, null))
                : Result<Response>.Success(new Response(assessment.ApplicabilityStatus, assessment.RuleSetVersion, assessment.Reasoning, assessment.AssessedAt));
        }
    }
}
