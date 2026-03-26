using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Users;

public static class CreateUser
{
    public record Command(string Email, string DisplayName, string Role) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "CreateUser";
        public string EntityType => "User";
    }

    public record Response(Guid Id, string Email, string DisplayName, string Role);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Role).Must(r => r is "SUPPLIER" or "BUYER" or "PLATFORM_ADMIN" or "TENANT_ADMIN")
                .WithMessage("Invalid role");
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IEmailService emailService, IConfiguration configuration, IPlanEnforcementService planEnforcement)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var admin = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid && u.IsActive, ct);
            if (admin is null)
                return Result<Response>.Failure("User not found");

            var callerRole = await currentUser.GetRoleAsync(ct);
            if (callerRole == Roles.TenantAdmin && cmd.Role is not ("SUPPLIER" or "BUYER"))
                return Result<Response>.Failure("You can only assign Supplier or Buyer roles");

            var limitError = await planEnforcement.CheckUserLimitAsync(admin.TenantId, ct);
            if (limitError is not null)
                return Result<Response>.Failure(limitError);

            var exists = await db.Users.AnyAsync(
                u => u.Email == cmd.Email && u.TenantId == admin.TenantId, ct);
            if (exists)
                return Result<Response>.Failure($"User with email '{cmd.Email}' already exists");

            var newUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                EntraOid = $"pending|{Guid.NewGuid()}", // Will be updated when user first logs in
                Email = cmd.Email,
                DisplayName = cmd.DisplayName,
                Role = cmd.Role,
                TenantId = admin.TenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct);

            var loginUrl = configuration["App:BaseUrl"] ?? "https://accutrac-web.onrender.com";
            await emailService.SendAsync(
                cmd.Email,
                "You've been invited to auditraks",
                $"<h2>Welcome to auditraks</h2><p>{cmd.DisplayName}, you've been invited to the auditraks supply chain compliance platform.</p><p><a href=\"{loginUrl}\">Sign in here</a></p>",
                $"{cmd.DisplayName}, you've been invited to auditraks. Sign in at {loginUrl}",
                ct);

            return Result<Response>.Success(new Response(
                newUser.Id, newUser.Email, newUser.DisplayName, newUser.Role));
        }
    }
}
