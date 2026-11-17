using AssetFlow.Api.Contracts;
using AssetFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController(IReportService reports) : ControllerBase
{
    /// <summary>Dashboard numbers: assets by status, pending and overdue loans, most borrowed assets.</summary>
    [HttpGet("summary")]
    public async Task<SummaryReport> Summary() => await reports.GetSummaryAsync();
}
