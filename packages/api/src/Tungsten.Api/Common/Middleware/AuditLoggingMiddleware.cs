using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Tungsten.Api.Common.Middleware;

public class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    private static readonly HashSet<string> WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!WriteMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        var bodyHash = await ComputeRequestBodyHash(context.Request);

        await next(context);

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        logger.LogInformation(
            "AUDIT: {Timestamp} | User: {UserId} | {Method} {Path} | BodyHash: {BodyHash} | Status: {StatusCode}",
            DateTime.UtcNow.ToString("O"), userId,
            context.Request.Method, context.Request.Path, bodyHash, context.Response.StatusCode);
    }

    private static async Task<string> ComputeRequestBodyHash(HttpRequest request)
    {
        request.Body.Position = 0;
        var body = await new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrEmpty(body)) return "empty";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexStringLower(hashBytes);
    }
}
