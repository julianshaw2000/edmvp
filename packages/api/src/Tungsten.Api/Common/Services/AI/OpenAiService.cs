using System.Net.Http.Json;
using System.Text.Json;

namespace Tungsten.Api.Common.Services.AI;

public class OpenAiService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<OpenAiService> logger) : IAiService
{
    public async Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var apiKey = config["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("OpenAI API key not configured, returning fallback");
            return "AI features are not configured. Please set the OpenAI API key.";
        }

        var model = config["OpenAI:Model"] ?? "gpt-4o-mini";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var request = new
            {
                model,
                max_tokens = 4096,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                }
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenAI API returned {Status}: {Body}", response.StatusCode, json);
                return $"AI service error: {response.StatusCode}";
            }

            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text ?? "No response generated.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI API call failed");
            return $"AI service temporarily unavailable: {ex.Message}";
        }
    }
}
