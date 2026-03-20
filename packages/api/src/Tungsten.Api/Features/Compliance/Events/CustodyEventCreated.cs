using System.Text.Json;
using MediatR;

namespace Tungsten.Api.Features.Compliance.Events;

public record CustodyEventCreated(
    Guid EventId,
    Guid BatchId,
    Guid TenantId,
    string EventType,
    string ActorName,
    string? SmelterId,
    JsonElement? Metadata,
    DateTime EventDate) : INotification;
