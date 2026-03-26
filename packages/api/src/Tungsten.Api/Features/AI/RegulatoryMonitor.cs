using MediatR;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;

namespace Tungsten.Api.Features.AI;

public static class RegulatoryMonitor
{
    public record Query : IRequest<Result<Response>>;
    public record Response(string Analysis, string DisclaimerNote);

    public class Handler(IAiService ai) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var systemPrompt = """
                You are a regulatory compliance expert specialising in 3TG (tungsten, tin, tantalum, gold) mineral supply chain regulations.
                You have deep knowledge of:
                - RMAP (Responsible Minerals Assurance Process)
                - OECD Due Diligence Guidance for Responsible Supply Chains (DDG)
                - Dodd-Frank Wall Street Reform Act Section 1502
                - EU Regulation 2017/821 on conflict minerals
                - SEC conflict minerals reporting rules

                Provide a structured analysis of recent or upcoming regulatory changes.
                Format as markdown with sections: Recent Changes, Upcoming Changes, Recommended Platform Actions.
                Be specific about effective dates and requirements. Note if information may be approximate due to knowledge cutoff.
                """;

            var userPrompt = """
                Based on your knowledge, are there any recent or upcoming changes to RMAP, OECD DDG, Dodd-Frank Section 1502,
                or EU Regulation 2017/821 that would affect 3TG mineral compliance for a custody tracking platform?

                Please list:
                1. Any recent changes and their effective dates
                2. Any upcoming changes that platform users should prepare for
                3. Specific actions the auditraks platform should take to remain compliant
                4. Any new reporting requirements or audit standards

                Focus on practical operational impact for supply chain participants (mines, smelters, buyers).
                """;

            var analysis = await ai.GenerateAsync(systemPrompt, userPrompt, ct);

            const string disclaimer = "This analysis is based on AI training data and may not reflect the most recent regulatory developments. " +
                "Always verify with official RMAP, OECD, SEC, and EU sources before making compliance decisions.";

            return Result<Response>.Success(new Response(analysis, disclaimer));
        }
    }
}
