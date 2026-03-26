using MediatR;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;

namespace Tungsten.Api.Features.AI;

public static class ChatAssistant
{
    public record Command(string Message, List<ChatMessage>? History) : IRequest<Result<Response>>;
    public record ChatMessage(string Role, string Content);
    public record Response(string Reply);

    public class Handler(IAiService ai) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var systemPrompt = """
                You are the auditraks help assistant. auditraks is a 3TG (tungsten, tin, tantalum, gold) mineral supply chain compliance platform.

                Key features:
                - SHA-256 hash chain custody event tracking
                - 5 automated compliance checks: RMAP smelter verification, OECD origin risk, sanctions screening, mass balance, event sequence
                - Material Passport PDF generation with QR codes
                - Multi-tenant SaaS with Supplier, Buyer, and Admin roles
                - Audit logging, analytics dashboard, API access

                Answer questions helpfully and concisely. If you don't know something specific about the user's data, suggest they check the relevant section of the dashboard.
                Keep responses under 200 words unless the question requires more detail.
                """;

            var userMessage = cmd.Message;
            if (cmd.History?.Count > 0)
            {
                var context = string.Join("\n", cmd.History.Select(h => $"{h.Role}: {h.Content}"));
                userMessage = $"Previous conversation:\n{context}\n\nUser: {cmd.Message}";
            }

            var reply = await ai.GenerateAsync(systemPrompt, userMessage, ct);
            return Result<Response>.Success(new Response(reply));
        }
    }
}
