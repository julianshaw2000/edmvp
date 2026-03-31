using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Buyer;

public static class ListCmrtImports
{
    public record Query : IRequest<Result<List<ImportItem>>>;

    public record ImportItem(
        Guid Id,
        string FileName,
        string DeclarationCompany,
        int? ReportingYear,
        int RowsParsed,
        int RowsMatched,
        int RowsUnmatched,
        string ImportedBy,
        DateTime ImportedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<List<ImportItem>>>
    {
        public async Task<Result<List<ImportItem>>> Handle(Query request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var imports = await db.CmrtImports.AsNoTracking()
                .Where(i => i.TenantId == tenantId)
                .OrderByDescending(i => i.ImportedAt)
                .Select(i => new ImportItem(
                    i.Id, i.FileName, i.DeclarationCompany, i.ReportingYear,
                    i.RowsParsed, i.RowsMatched, i.RowsUnmatched,
                    i.Importer.DisplayName, i.ImportedAt))
                .ToListAsync(ct);

            return Result<List<ImportItem>>.Success(imports);
        }
    }
}
