using ClosedXML.Excel;

namespace Tungsten.Api.Common.Services;

public record CmrtDeclaration(string CompanyName, int? ReportingYear, string? DeclarationScope);

public record CmrtSmelterRow(
    string MetalType,
    string? SmelterName,
    string? SmelterId,
    string? Country,
    string? SourceCountry,
    string? SourcingStatus,
    int RowNumber);

public record CmrtParseResult(
    CmrtDeclaration Declaration,
    List<CmrtSmelterRow> Smelters,
    List<string> Errors);

public static class CmrtParserService
{
    public static CmrtParseResult Parse(Stream fileStream)
    {
        var errors = new List<string>();
        var smelters = new List<CmrtSmelterRow>();
        string companyName = "Unknown";
        int? reportingYear = null;
        string? declarationScope = null;

        using var workbook = new XLWorkbook(fileStream);

        var declSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("Declaration", StringComparison.OrdinalIgnoreCase));

        if (declSheet is not null)
        {
            companyName = declSheet.Cell("B8").GetString()?.Trim() ?? "Unknown";
            if (string.IsNullOrWhiteSpace(companyName)) companyName = "Unknown";

            var yearCell = declSheet.Cell("B18").GetString()?.Trim();
            if (int.TryParse(yearCell, out var year)) reportingYear = year;

            var scopeCell = declSheet.Cell("B14").GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(scopeCell)) declarationScope = scopeCell;
        }
        else
        {
            errors.Add("Declaration worksheet not found");
        }

        var smelterSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("Smelter", StringComparison.OrdinalIgnoreCase)
            && ws.Name.Contains("List", StringComparison.OrdinalIgnoreCase));

        if (smelterSheet is not null)
        {
            var lastRow = smelterSheet.LastRowUsed()?.RowNumber() ?? 3;

            for (var row = 4; row <= lastRow; row++)
            {
                var metalType = smelterSheet.Cell(row, 1).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(metalType)) continue;

                var smelterName = smelterSheet.Cell(row, 2).GetString()?.Trim();
                var smelterId = smelterSheet.Cell(row, 3).GetString()?.Trim();
                var country = smelterSheet.Cell(row, 4).GetString()?.Trim();
                var sourceCountry = smelterSheet.Cell(row, 5).GetString()?.Trim();
                var sourcingStatus = smelterSheet.Cell(row, 6).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(smelterName) && string.IsNullOrWhiteSpace(smelterId))
                {
                    errors.Add($"Row {row}: Missing both smelter name and ID");
                    continue;
                }

                smelters.Add(new CmrtSmelterRow(
                    metalType, smelterName, smelterId, country,
                    sourceCountry, sourcingStatus, row));
            }
        }
        else
        {
            errors.Add("Smelter List worksheet not found");
        }

        return new CmrtParseResult(
            new CmrtDeclaration(companyName, reportingYear, declarationScope),
            smelters,
            errors);
    }
}
