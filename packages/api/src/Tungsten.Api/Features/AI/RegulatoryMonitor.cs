using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;

namespace Tungsten.Api.Features.AI;

public static class RegulatoryMonitor
{
    public record Query : IRequest<Result<Response>>;
    public record Response(string Analysis, string Sources, string LastChecked);

    public class Handler(IAiService ai, IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<Handler> logger)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            // Step 1: Search for current regulatory news via Tavily
            var searchResults = await SearchRegulatoryUpdates(ct);

            // Step 2: Send search results to AI for analysis
            var systemPrompt = """
                You are a regulatory compliance expert specialising in 3TG (tungsten, tin, tantalum, gold) mineral supply chain regulations.
                You have deep knowledge of RMAP, OECD DDG, Dodd-Frank Section 1502, and EU Regulation 2017/821.

                You will be given CURRENT search results from the web about regulatory changes.
                Analyze these results and provide a structured report.
                Format as markdown with sections:
                ## Recent Changes
                ## Upcoming Changes
                ## Impact on auditraks Platform
                ## Recommended Actions

                Be specific about dates, requirements, and practical impact.
                Only reference information found in the search results — do not make up changes.
                If the search results don't contain relevant changes, say so clearly.
                """;

            var userPrompt = $"""
                Here are current search results about 3TG mineral compliance regulatory changes:

                {searchResults}

                Analyze these for any changes to:
                1. RMAP (Responsible Minerals Assurance Process) — smelter certification, audit requirements
                2. OECD Due Diligence Guidance — supply chain due diligence standards
                3. Dodd-Frank Section 1502 — SEC conflict minerals reporting
                4. EU Regulation 2017/821 — EU conflict minerals importation rules
                5. Any other relevant 3TG compliance regulations

                Focus on practical operational impact for a compliance tracking platform and its users.
                """;

            var analysis = await ai.GenerateAsync(systemPrompt, userPrompt, ct);

            return Result<Response>.Success(new Response(
                analysis,
                "Data sourced from live web search via Tavily API",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC")));
        }

        private async Task<string> SearchRegulatoryUpdates(CancellationToken ct)
        {
            var tavilyKey = config["Tavily:ApiKey"];
            if (string.IsNullOrEmpty(tavilyKey))
            {
                logger.LogWarning("Tavily API key not configured, skipping web search");
                return "No web search results available — Tavily API key not configured.";
            }

            var searches = new[]
            {
                "RMAP responsible minerals assurance process 2025 2026 changes updates",
                "OECD due diligence guidance 3TG conflict minerals 2025 2026 updates",
                "Dodd-Frank section 1502 conflict minerals SEC 2025 2026 changes",
                "EU regulation 2017/821 conflict minerals 2025 2026 updates"
            };

            var allResults = new List<string>();

            var client = httpClientFactory.CreateClient();

            foreach (var searchQuery in searches)
            {
                try
                {
                    var request = new
                    {
                        api_key = tavilyKey,
                        query = searchQuery,
                        max_results = 3,
                        search_depth = "basic"
                    };

                    var response = await client.PostAsJsonAsync("https://api.tavily.com/search", request, ct);
                    var json = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("results", out var results))
                        {
                            foreach (var result in results.EnumerateArray())
                            {
                                var title = result.TryGetProperty("title", out var t) ? t.GetString() : "";
                                var content = result.TryGetProperty("content", out var c) ? c.GetString() : "";
                                var url = result.TryGetProperty("url", out var u) ? u.GetString() : "";
                                allResults.Add($"**{title}**\nSource: {url}\n{content}\n");
                            }
                        }
                    }
                    else
                    {
                        logger.LogWarning("Tavily search failed for '{Query}': {Status}", searchQuery, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Tavily search failed for '{Query}'", searchQuery);
                }
            }

            if (allResults.Count == 0)
                return "No search results found. Regulatory sources may be temporarily unavailable.";

            return string.Join("\n---\n", allResults);
        }
    }
}
