using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace Tungsten.Api.Common.Services.AI;

public class ClaudeAiService(IConfiguration config, ILogger<ClaudeAiService> logger) : IAiService
{
    public async Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("Anthropic API key not configured, returning fallback");
            return "AI features are not configured. Please set the Anthropic API key.";
        }

        var model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

        try
        {
            var client = new AnthropicClient(apiKey);
            var parameters = new MessageParameters
            {
                Model = model,
                MaxTokens = 4096,
                System = [new SystemMessage(systemPrompt)],
                Messages = [new Message(RoleType.User, userMessage)],
            };

            var response = await client.Messages.GetClaudeMessageAsync(parameters, ct);
            var textBlock = response.Content?.OfType<Anthropic.SDK.Messaging.TextContent>().FirstOrDefault();
            return textBlock?.Text ?? "No response generated.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude API call failed");
            return $"AI service temporarily unavailable: {ex.Message}";
        }
    }
}
