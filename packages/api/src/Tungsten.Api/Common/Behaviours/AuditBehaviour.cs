using MediatR;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Common.Behaviours;

public class AuditBehaviour<TRequest, TResponse>(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IAuditable auditable)
            return await next();

        if (httpContextAccessor.HttpContext is null)
            return await next();

        var response = await next();

        try
        {
            var userId = await currentUser.GetUserIdAsync(ct);
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            Guid? entityId = null;
            string? failureReason = null;
            var resultText = "Success";

            // Handle non-generic Result
            if (response is Result nonGenericResult)
            {
                if (!nonGenericResult.IsSuccess)
                {
                    resultText = "Failure";
                    failureReason = nonGenericResult.Error;
                }
            }
            else if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccess = (bool)typeof(TResponse).GetProperty("IsSuccess")!.GetValue(response)!;
                if (isSuccess)
                {
                    var value = typeof(TResponse).GetProperty("Value")!.GetValue(response);
                    if (value is not null)
                    {
                        var idProp = value.GetType().GetProperty("Id");
                        if (idProp?.PropertyType == typeof(Guid))
                            entityId = (Guid)idProp.GetValue(value)!;
                    }
                }
                else
                {
                    resultText = "Failure";
                    failureReason = typeof(TResponse).GetProperty("Error")?.GetValue(response)?.ToString();
                }
            }

            // Extract BatchId
            Guid? batchId = null;
            if (auditable.EntityType == "Batch")
            {
                batchId = entityId;
            }
            else
            {
                var batchIdProp = typeof(TRequest).GetProperty("BatchId");
                if (batchIdProp?.PropertyType == typeof(Guid))
                    batchId = (Guid)batchIdProp.GetValue(request)!;
            }

            var httpContext = httpContextAccessor.HttpContext;
            var entry = new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Action = auditable.AuditAction,
                EntityType = auditable.EntityType,
                EntityId = entityId,
                BatchId = batchId,
                Payload = AuditPayloadSerializer.Serialize(request),
                Result = resultText,
                FailureReason = failureReason,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString().Length > 500
                    ? httpContext.Request.Headers.UserAgent.ToString()[..500]
                    : httpContext.Request.Headers.UserAgent.ToString(),
                Timestamp = DateTime.UtcNow,
            };

            db.AuditLogs.Add(entry);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for {Action}", auditable.AuditAction);
        }

        return response;
    }
}
