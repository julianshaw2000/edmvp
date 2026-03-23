using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tungsten.Api.Common;

namespace Tungsten.Api.Features.DocumentGeneration.Templates;

public record DossierData(
    string BatchNumber,
    string TenantName,
    string MineralType,
    string OriginCountry,
    string OriginMine,
    decimal WeightKg,
    string Status,
    string ComplianceStatus,
    IReadOnlyList<DossierEventData> Events,
    IReadOnlyList<DossierComplianceData> ComplianceChecks,
    IReadOnlyList<DossierDocumentData> Documents,
    bool HashChainIntact,
    string GeneratedByDisplayName,
    DateTime GeneratedAt);

public record DossierEventData(
    string EventType, DateTime EventDate, string Location,
    string ActorName, string? SmelterId, string Description,
    bool IsCorrection, Guid? CorrectsEventId,
    string Sha256Hash, string? PreviousEventHash);

public record DossierComplianceData(
    string EventType, string Framework, string Status,
    string Details, DateTime CheckedAt);

public record DossierDocumentData(
    string FileName, string DocumentType, long FileSizeBytes,
    string UploadedBy, DateTime CreatedAt, string Sha256Hash);

public class DossierTemplate(DossierData data) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Text("AUDIT DOSSIER").Bold().FontSize(18);
                col.Item().PaddingTop(5).Text($"Batch: {data.BatchNumber} | {data.TenantName}").FontSize(12).Bold();
                col.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}").FontSize(8).FontColor(Colors.Grey.Medium);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                // Batch Summary
                col.Item().Text("1. Batch Summary").Bold().FontSize(11);
                col.Item().PaddingTop(3).Text($"Mineral: {data.MineralType} | Origin: {data.OriginCountry}, {data.OriginMine} | Weight: {data.WeightKg:N2} kg");
                col.Item().Text($"Status: {data.Status} | Compliance: {data.ComplianceStatus}");

                // Full Event Log
                col.Item().PaddingTop(12).Text("2. Full Event Log").Bold().FontSize(11);
                foreach (var evt in data.Events)
                {
                    col.Item().PaddingTop(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(evtCol =>
                    {
                        evtCol.Item().Text($"{evt.EventType} — {evt.EventDate:yyyy-MM-dd HH:mm}").Bold();
                        evtCol.Item().Text($"Location: {evt.Location} | Actor: {evt.ActorName}");
                        if (evt.SmelterId is not null)
                            evtCol.Item().Text($"Smelter ID: {evt.SmelterId}");
                        evtCol.Item().Text($"Description: {evt.Description}");
                        if (evt.IsCorrection)
                            evtCol.Item().Text($"CORRECTION of event {evt.CorrectsEventId}").FontColor(Colors.Orange.Darken2);
                        evtCol.Item().Text($"Hash: {evt.Sha256Hash}").FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                }

                // Compliance Details
                col.Item().PaddingTop(12).Text("3. Compliance Check Details").Bold().FontSize(11);
                col.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(1);
                        c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Event").Bold();
                        h.Cell().Text("Framework").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().Text("Details").Bold();
                        h.Cell().Text("Checked").Bold();
                    });
                    foreach (var check in data.ComplianceChecks)
                    {
                        table.Cell().Text(check.EventType);
                        table.Cell().Text(check.Framework);
                        var statusColor = check.Status switch
                        {
                            "FAIL" => Colors.Red.Darken2,
                            "FLAG" => Colors.Orange.Darken2,
                            _ => Colors.Black
                        };
                        table.Cell().Text(check.Status).FontColor(statusColor);
                        table.Cell().Text(check.Details).FontSize(8);
                        table.Cell().Text(check.CheckedAt.ToString("yyyy-MM-dd"));
                    }
                });

                // Document List
                col.Item().PaddingTop(12).Text("4. Document List").Bold().FontSize(11);
                col.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); c.RelativeColumn(2);
                        c.RelativeColumn(1); c.RelativeColumn(2);
                        c.RelativeColumn(2); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("File").Bold();
                        h.Cell().Text("Type").Bold();
                        h.Cell().Text("Size").Bold();
                        h.Cell().Text("Uploaded By").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("SHA-256").Bold();
                    });
                    foreach (var doc in data.Documents)
                    {
                        table.Cell().Text(doc.FileName);
                        table.Cell().Text(doc.DocumentType);
                        table.Cell().Text(FormatSize(doc.FileSizeBytes));
                        table.Cell().Text(doc.UploadedBy);
                        table.Cell().Text(doc.CreatedAt.ToString("yyyy-MM-dd"));
                        table.Cell().Text(doc.Sha256Hash.Length >= 16
                            ? doc.Sha256Hash[..16] + "…"
                            : doc.Sha256Hash).FontSize(7).FontColor(Colors.Grey.Medium);
                    }
                });

                // Hash Chain Integrity
                col.Item().PaddingTop(12).Text("5. Tamper Evidence").Bold().FontSize(11);
                col.Item().PaddingTop(5).Text(data.HashChainIntact
                    ? "Hash chain verification: INTACT — No tampering detected"
                    : "Hash chain verification: BROKEN — Potential tampering detected")
                    .FontColor(data.HashChainIntact ? Colors.Green.Darken2 : Colors.Red.Darken2);
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span($"Generated by auditraks v{PlatformInfo.Version} | Rule Set v{PlatformInfo.RuleVersion} | ");
                text.Span(data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                text.Span($" | {data.GeneratedByDisplayName}");
            });
        });
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
