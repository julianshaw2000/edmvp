using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Batches;

public static class CreateBatch
{
    public record Command(
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg,
        string Status,
        string ComplianceStatus,
        DateTime CreatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(100);
            RuleFor(x => x.MineralType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.OriginCountry).NotEmpty().Length(2)
                .Matches("^[A-Z]{2}$").WithMessage("Must be ISO 3166-1 alpha-2");
            RuleFor(x => x.OriginMine).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WeightKg).GreaterThan(0);
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var exists = await db.Batches.AnyAsync(
                b => b.TenantId == user.TenantId && b.BatchNumber == cmd.BatchNumber, ct);
            if (exists)
                return Result<Response>.Failure($"Batch '{cmd.BatchNumber}' already exists in this tenant");

            var batch = new BatchEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                BatchNumber = cmd.BatchNumber,
                MineralType = cmd.MineralType,
                OriginCountry = cmd.OriginCountry,
                OriginMine = cmd.OriginMine,
                WeightKg = cmd.WeightKg,
                Status = "CREATED",
                ComplianceStatus = "PENDING",
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Batches.Add(batch);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                batch.Id, batch.BatchNumber, batch.MineralType,
                batch.OriginCountry, batch.OriginMine, batch.WeightKg,
                batch.Status, batch.ComplianceStatus, batch.CreatedAt));
        }
    }
}
