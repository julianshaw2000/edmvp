using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Admin;

public static class ListJobs
{
    public record Query : IRequest<Result<Response>>;

    public record JobItem(
        Guid Id,
        string JobType,
        string Status,
        Guid ReferenceId,
        string? ErrorDetail,
        DateTime CreatedAt,
        DateTime? CompletedAt);

    public record Response(IReadOnlyList<JobItem> Jobs, int TotalCount);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var jobs = await db.Jobs.AsNoTracking()
                .OrderByDescending(j => j.CreatedAt)
                .Take(50)
                .Select(j => new JobItem(
                    j.Id,
                    j.JobType,
                    j.Status,
                    j.ReferenceId,
                    j.ErrorDetail,
                    j.CreatedAt,
                    j.CompletedAt))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(jobs, jobs.Count));
        }
    }
}
