using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.CustodyEvents;

public static class CreateCorrection
{
    public record Command(
        Guid OriginalEventId,
        string EventDate,
        string Location,
        string? GpsCoordinates,
        string ActorName,
        string? SmelterId,
        string Description,
        JsonElement Metadata) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string EventType,
        bool IsCorrection,
        Guid? CorrectsEventId,
        string Sha256Hash,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IPublisher publisher)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var originalEvent = await db.CustodyEvents
                .FirstOrDefaultAsync(e => e.Id == cmd.OriginalEventId && e.TenantId == user.TenantId, ct);
            if (originalEvent is null)
                return Result<Response>.Failure("Original event not found");

            // Validate metadata against original event type
            var metadataValidation = MetadataValidator.Validate(originalEvent.EventType, cmd.Metadata);
            if (!metadataValidation.IsValid)
                return Result<Response>.Failure($"Invalid metadata: {string.Join("; ", metadataValidation.Errors)}");

            // Correction gets its own idempotency key (original event id + correction timestamp)
            var idempotencyKey = IdempotencyKeyService.GenerateKey(
                originalEvent.BatchId, originalEvent.EventType, cmd.EventDate, cmd.Location, cmd.ActorName);

            var exists = await db.CustodyEvents.AnyAsync(
                e => e.BatchId == originalEvent.BatchId && e.IdempotencyKey == idempotencyKey, ct);
            if (exists)
                return Result<Response>.Failure("Duplicate correction event");

            var previousHash = await db.CustodyEvents
                .Where(e => e.BatchId == originalEvent.BatchId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Sha256Hash)
                .FirstOrDefaultAsync(ct);

            var metadataString = cmd.Metadata.GetRawText();
            var normalizedDate = HashService.NormalizeDate(cmd.EventDate);
            var sha256Hash = HashService.ComputeEventHash(
                originalEvent.EventType, normalizedDate, originalEvent.BatchId,
                cmd.Location, cmd.ActorName, cmd.SmelterId,
                cmd.Description, metadataString, previousHash);

            var entity = new CustodyEventEntity
            {
                Id = Guid.NewGuid(),
                BatchId = originalEvent.BatchId,
                TenantId = user.TenantId,
                EventType = originalEvent.EventType,
                IdempotencyKey = idempotencyKey,
                EventDate = DateTime.Parse(cmd.EventDate).ToUniversalTime(),
                Location = cmd.Location,
                GpsCoordinates = cmd.GpsCoordinates,
                ActorName = cmd.ActorName,
                SmelterId = cmd.SmelterId,
                Description = cmd.Description,
                Metadata = cmd.Metadata,
                SchemaVersion = 1,
                IsCorrection = true,
                CorrectsEventId = originalEvent.Id,
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
                true, entity.CorrectsEventId,
                entity.Sha256Hash, entity.CreatedAt));
        }
    }
}
