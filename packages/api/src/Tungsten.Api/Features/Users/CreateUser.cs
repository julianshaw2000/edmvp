using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Users;

public static class CreateUser
{
    public record Command(string Email, string DisplayName, string Role) : IRequest<Result<Response>>;

    public record Response(Guid Id, string Email, string DisplayName, string Role);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Role).NotEmpty().Must(r => r is "SUPPLIER" or "BUYER" or "PLATFORM_ADMIN")
                .WithMessage("Role must be SUPPLIER, BUYER, or PLATFORM_ADMIN");
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IEmailService emailService, IConfiguration configuration)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var admin = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (admin is null)
                return Result<Response>.Failure("User not found");

            var exists = await db.Users.AnyAsync(
                u => u.Email == cmd.Email && u.TenantId == admin.TenantId, ct);
            if (exists)
                return Result<Response>.Failure($"User with email '{cmd.Email}' already exists");

            var newUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                Auth0Sub = $"pending|{Guid.NewGuid()}", // Will be updated when user first logs in
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
                "You've been invited to AccuTrac",
                $"<h2>Welcome to AccuTrac</h2><p>{cmd.DisplayName}, you've been invited to the AccuTrac supply chain compliance platform.</p><p><a href=\"{loginUrl}\">Sign in here</a></p>",
                $"{cmd.DisplayName}, you've been invited to AccuTrac. Sign in at {loginUrl}",
                ct);

            return Result<Response>.Success(new Response(
                newUser.Id, newUser.Email, newUser.DisplayName, newUser.Role));
        }
    }
}
