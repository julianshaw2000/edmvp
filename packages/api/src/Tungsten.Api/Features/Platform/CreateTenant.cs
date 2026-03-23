using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Platform;

public static class CreateTenant
{
    public record Command(string Name, string AdminEmail) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "CreateTenant";
        public string EntityType => "Tenant";
    }

    public record Response(Guid Id, string Name, string Status, string AdminEmail, DateTime CreatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        }
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var emailExists = await db.Users.AnyAsync(u => u.Email == cmd.AdminEmail, ct);
            if (emailExists)
                return Result<Response>.Failure($"Email '{cmd.AdminEmail}' is already in use");

            var basePrefix = GenerateSchemaPrefix(cmd.Name);
            var prefix = basePrefix;
            var suffix = 2;
            while (await db.Tenants.AnyAsync(t => t.SchemaPrefix == prefix, ct))
            {
                prefix = $"{basePrefix}_{suffix}";
                suffix++;
            }

            var tenant = new TenantEntity
            {
                Id = Guid.NewGuid(),
                Name = cmd.Name,
                SchemaPrefix = prefix,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            };

            var adminUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                Auth0Sub = $"pending|{Guid.NewGuid()}",
                Email = cmd.AdminEmail,
                DisplayName = cmd.AdminEmail.Split('@')[0],
                Role = "TENANT_ADMIN",
                TenantId = tenant.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Tenants.Add(tenant);
            db.Users.Add(adminUser);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(
                new Response(tenant.Id, tenant.Name, tenant.Status, cmd.AdminEmail, tenant.CreatedAt));
        }

        private static string GenerateSchemaPrefix(string name)
        {
            var prefix = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            return prefix.Length > 50 ? prefix[..50] : prefix;
        }
    }
}
