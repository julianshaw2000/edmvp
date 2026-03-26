namespace Tungsten.Api.Common.Services.AI;

public interface IAiService
{
    Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}
