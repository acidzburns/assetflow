using AssetFlow.Api.Contracts;
using AssetFlow.Api.Data;
using AssetFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Api.Services;

public interface IReportService
{
    Task<SummaryReport> GetSummaryAsync();
}

/// <summary>
/// Dashboard numbers. The grouping queries are written in plain SQL on purpose,
/// to show the SQL behind the report (GROUP BY, JOIN, ORDER BY, LIMIT).
/// </summary>
public class ReportService(AppDbContext db, TimeProvider clock) : IReportService
{
    public async Task<SummaryReport> GetSummaryAsync()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var byStatus = await db.Database.SqlQueryRaw<StatusCount>("""
            SELECT "Status", COUNT(*) AS "Count"
            FROM "Assets"
            GROUP BY "Status"
            ORDER BY "Status"
            """).ToListAsync();

        var mostBorrowed = await db.Database.SqlQueryRaw<TopAsset>("""
            SELECT a."Tag", a."Name", COUNT(l."Id") AS "LoanCount"
            FROM "Assets" a
            JOIN "Loans" l ON l."AssetId" = a."Id"
            WHERE l."Status" IN ('CheckedOut', 'Returned')
            GROUP BY a."Id", a."Tag", a."Name"
            ORDER BY "LoanCount" DESC, a."Tag"
            LIMIT 5
            """).ToListAsync();

        var pending = await db.Loans.CountAsync(l => l.Status == LoanStatus.Requested);
        var active = await db.Loans.CountAsync(l => l.Status == LoanStatus.CheckedOut);
        var overdue = await db.Loans.CountAsync(l => l.Status == LoanStatus.CheckedOut && l.DueDate < today);

        var repairCosts = await db.MaintenanceRecords
            .Where(m => m.ClosedAt >= yearStart && m.Cost != null)
            .Select(m => m.Cost!.Value)
            .ToListAsync(); // summed in C# so it works the same on every database

        return new SummaryReport(byStatus, pending, active, overdue, mostBorrowed, repairCosts.Sum());
    }
}
