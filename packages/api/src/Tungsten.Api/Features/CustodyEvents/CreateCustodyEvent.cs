using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.CustodyEvents;

public static class CreateCustodyEvent
{
    public record Command(
        Guid BatchId,
        string EventType,
        string EventDate,
        string Location,
        string? GpsCoordinates,
        string ActorName,
        string? SmelterId,
        string Description,
        JsonElement Metadata) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "CreateCustodyEvent";
        public string EntityType => "CustodyEvent";
    }

    public record Response(
        Guid Id,
        Guid BatchId,
        string EventType,
        string EventDate,
        string Location,
        string ActorName,
        string Sha256Hash,
        string? PreviousEventHash,
        DateTime CreatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BatchId).NotEmpty();
            RuleFor(x => x.EventType).NotEmpty().MaximumLength(30);
            RuleFor(x => x.EventDate).NotEmpty();
            RuleFor(x => x.Location).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ActorName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.GpsCoordinates)
                .Must(BeValidGpsCoordinates)
                .When(x => !string.IsNullOrEmpty(x.GpsCoordinates))
                .WithMessage("GpsCoordinates must be in 'lat,lon' format where lat is -90 to 90 and lon is -180 to 180");
        }

        private static bool BeValidGpsCoordinates(string? value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            var parts = value.Split(',');
            if (parts.Length != 2) return false;
            if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat)) return false;
            if (!double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon)) return false;
            return lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IPublisher publisher)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            // Validate metadata for event type
            var metadataValidation = MetadataValidator.Validate(cmd.EventType, cmd.Metadata);
            if (!metadataValidation.IsValid)
                return Result<Response>.Failure($"Invalid metadata: {string.Join("; ", metadataValidation.Errors)}");

            // Generate idempotency key
            var idempotencyKey = IdempotencyKeyService.GenerateKey(
                cmd.BatchId, cmd.EventType, cmd.EventDate, cmd.Location, cmd.ActorName);

            // Check for duplicate
            var exists = await db.CustodyEvents.AnyAsync(
                e => e.BatchId == cmd.BatchId && e.IdempotencyKey == idempotencyKey, ct);
            if (exists)
                return Result<Response>.Failure("Duplicate event: an event with these key fields already exists for this batch");

            // Get previous event hash for chain
            var previousHash = await db.CustodyEvents
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Sha256Hash)
                .FirstOrDefaultAsync(ct);

            // Normalize date and compute SHA-256 hash
            var normalizedDate = HashService.NormalizeDate(cmd.EventDate);
            var metadataString = cmd.Metadata.GetRawText();
            var sha256Hash = HashService.ComputeEventHash(
                cmd.EventType, normalizedDate, cmd.BatchId, cmd.Location,
                cmd.ActorName, cmd.SmelterId, cmd.Description,
                metadataString, previousHash);

            var entity = new CustodyEventEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                EventType = cmd.EventType,
                IdempotencyKey = idempotencyKey,
                EventDate = DateTime.Parse(cmd.EventDate).ToUniversalTime(),
                Location = cmd.Location,
                GpsCoordinates = cmd.GpsCoordinates,
                ActorName = cmd.ActorName,
                SmelterId = cmd.SmelterId,
                Description = cmd.Description,
                Metadata = cmd.Metadata,
                SchemaVersion = 1,
                IsCorrection = false,
                Sha256Hash = sha256Hash,
                PreviousEventHash = previousHash,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            db.CustodyEvents.Add(entity);
            await db.SaveChangesAsync(ct);

            await publisher.Publish(new CustodyEventCreated(
                entity.Id, entity.BatchId, entity.TenantId,
                entity.EventType, entity.ActorName, entity.SmelterId,
                entity.Metadata, entity.EventDate), ct);

            return Result<Response>.Success(new Response(
                entity.Id, entity.BatchId, entity.EventType,
                cmd.EventDate, entity.Location, entity.ActorName,
                entity.Sha256Hash, entity.PreviousEventHash,
                entity.CreatedAt));
        }
    }
}
