using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tungsten.Api.Common;

namespace Tungsten.Api.Features.DocumentGeneration.Templates;

public record PassportData(
    string BatchNumber,
    string TenantName,
    string MineralType,
    string OriginCountry,
    string OriginMine,
    decimal WeightKg,
    string Status,
    string ComplianceStatus,
    string VerificationUrl,
    IReadOnlyList<PassportEventData> Events,
    IReadOnlyList<PassportComplianceData> ComplianceChecks,
    IReadOnlyList<PassportDocumentData> Documents,
    bool HashChainIntact,
    string GeneratedByDisplayName,
    DateTime GeneratedAt);

public record PassportEventData(
    string EventType, DateTime EventDate, string Location,
    string ActorName, bool IsCorrection, string Sha256Hash);

public record PassportComplianceData(
    string EventType, string Framework, string Status, DateTime CheckedAt);

public record PassportDocumentData(
    string FileName, string DocumentType, DateTime CreatedAt);

public class PassportTemplate(PassportData data) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("MATERIAL PASSPORT").Bold().FontSize(18);
                    row.ConstantItem(150).AlignRight().Text(data.TenantName).FontSize(10);
                });
                col.Item().PaddingTop(5).Text($"Batch: {data.BatchNumber}").FontSize(12).Bold();
                col.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}").FontSize(8).FontColor(Colors.Grey.Medium);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().PaddingTop(15).Column(col =>
            {
                // Batch Summary
                col.Item().Text("Batch Summary").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    table.Cell().Text("Mineral Type:"); table.Cell().Text(data.MineralType);
                    table.Cell().Text("Origin Country:"); table.Cell().Text(data.OriginCountry);
                    table.Cell().Text("Origin Mine:"); table.Cell().Text(data.OriginMine);
                    table.Cell().Text("Weight (kg):"); table.Cell().Text(data.WeightKg.ToString("N2"));
                    table.Cell().Text("Status:"); table.Cell().Text(data.Status);
                    table.Cell().Text("Compliance:"); table.Cell().Text(data.ComplianceStatus);
                });

                // Custody Chain
                col.Item().PaddingTop(15).Text("Custody Chain").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(2); c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Event Type").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Location").Bold();
                        h.Cell().Text("Actor").Bold();
                        h.Cell().Text("Correction").Bold();
                    });
                    foreach (var evt in data.Events)
                    {
                        table.Cell().Text(evt.EventType);
                        table.Cell().Text(evt.EventDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(evt.Location);
                        table.Cell().Text(evt.ActorName);
                        table.Cell().Text(evt.IsCorrection ? "Yes" : "");
                    }
                });

                // Compliance Summary
                col.Item().PaddingTop(15).Text("Compliance Summary").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(1); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Event Type").Bold();
                        h.Cell().Text("Framework").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().Text("Checked").Bold();
                    });
                    foreach (var check in data.ComplianceChecks)
                    {
                        table.Cell().Text(check.EventType);
                        table.Cell().Text(check.Framework);
                        table.Cell().Text(check.Status);
                        table.Cell().Text(check.CheckedAt.ToString("yyyy-MM-dd"));
                    }
                });

                // Document Registry
                col.Item().PaddingTop(15).Text("Document Registry").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("File Name").Bold();
                        h.Cell().Text("Type").Bold();
                        h.Cell().Text("Uploaded").Bold();
                    });
                    foreach (var doc in data.Documents)
                    {
                        table.Cell().Text(doc.FileName);
                        table.Cell().Text(doc.DocumentType);
                        table.Cell().Text(doc.CreatedAt.ToString("yyyy-MM-dd"));
                    }
                });

                // Tamper Evidence
                col.Item().PaddingTop(15).Text("Tamper Evidence").Bold().FontSize(12);
                col.Item().PaddingTop(5).Text(data.HashChainIntact
                    ? "Hash chain verification: INTACT - No tampering detected"
                    : "Hash chain verification: BROKEN - Potential tampering detected")
                    .FontColor(data.HashChainIntact ? Colors.Green.Darken2 : Colors.Red.Darken2);

                // Verification URL with QR code placeholder
                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("Verify online:").FontSize(8).Bold();
                        inner.Item().Text(data.VerificationUrl).FontSize(8);
                    });
                    row.ConstantItem(80).Border(1).BorderColor(Colors.Grey.Medium)
                        .Padding(4).AlignCenter().AlignMiddle()
                        .Column(qr =>
                        {
                            qr.Item().AlignCenter().Text("[QR CODE]").FontSize(6).Bold();
                            qr.Item().AlignCenter().Text(data.VerificationUrl)
                                .FontSize(4).FontColor(Colors.Grey.Darken1);
                        });
                });
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span($"Generated by AccuTrac v{PlatformInfo.Version} | Rule Set v{PlatformInfo.RuleVersion} | ");
                text.Span(data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                text.Span($" | {data.GeneratedByDisplayName}");
            });
        });
    }
}
