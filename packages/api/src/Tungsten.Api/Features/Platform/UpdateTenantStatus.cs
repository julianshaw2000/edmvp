using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Platform;

public static class UpdateTenantStatus
{
    public record Command(Guid TenantId, string Status) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "UpdateTenantStatus";
        public string EntityType => "Tenant";
    }

    public record Response(Guid Id, string Name, string Status);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Status).Must(s => s is "ACTIVE" or "SUSPENDED")
                .WithMessage("Status must be ACTIVE or SUSPENDED");
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == cmd.TenantId, ct);
            if (tenant is null)
                return Result<Response>.Failure("Tenant not found");

            if (cmd.Status == "SUSPENDED")
            {
                var callerTenantId = await currentUser.GetTenantIdAsync(ct);
                if (callerTenantId == cmd.TenantId)
                    return Result<Response>.Failure("Cannot suspend your own tenant");
            }

            tenant.Status = cmd.Status;
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(tenant.Id, tenant.Name, tenant.Status));
        }
    }
}
