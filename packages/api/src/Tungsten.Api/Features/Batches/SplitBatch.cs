using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Batches;

public static class SplitBatch
{
    public record Command(Guid BatchId, decimal ChildAWeightKg, decimal ChildBWeightKg) : IRequest<Result<Response>>;

    public record Response(Guid ChildAId, string ChildABatchNumber, Guid ChildBId, string ChildBBatchNumber);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChildAWeightKg).GreaterThan(0);
            RuleFor(x => x.ChildBWeightKg).GreaterThan(0);
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var parent = await db.Batches
                .FirstOrDefaultAsync(b => b.Id == request.BatchId && b.TenantId == user.TenantId, ct);
            if (parent is null)
                return Result<Response>.Failure("Batch not found");

            if (parent.Status == "COMPLETED")
                return Result<Response>.Failure("Cannot split a completed batch");

            var totalWeight = request.ChildAWeightKg + request.ChildBWeightKg;
            if (Math.Abs(totalWeight - parent.WeightKg) > 0.01m)
                return Result<Response>.Failure("Child weights must sum to parent weight");

            var childA = new BatchEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                BatchNumber = $"{parent.BatchNumber}-A",
                MineralType = parent.MineralType,
                OriginCountry = parent.OriginCountry,
                OriginMine = parent.OriginMine,
                WeightKg = request.ChildAWeightKg,
                Status = "CREATED",
                ComplianceStatus = "PENDING",
                ParentBatchId = parent.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var childB = new BatchEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                BatchNumber = $"{parent.BatchNumber}-B",
                MineralType = parent.MineralType,
                OriginCountry = parent.OriginCountry,
                OriginMine = parent.OriginMine,
                WeightKg = request.ChildBWeightKg,
                Status = "CREATED",
                ComplianceStatus = "PENDING",
                ParentBatchId = parent.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Batches.AddRange(childA, childB);
            parent.Status = "COMPLETED";
            parent.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                childA.Id, childA.BatchNumber, childB.Id, childB.BatchNumber));
        }
    }
}
