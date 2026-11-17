using AssetFlow.Api.Contracts;
using AssetFlow.Api.Domain;
using AssetFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.Api.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController(IAssetService assets) : ControllerBase
{
    /// <summary>Search assets by tag, name or serial number, and filter by status or category.</summary>
    [HttpGet]
    public async Task<PagedResult<AssetDto>> Search(
        string? search, AssetStatus? status, AssetCategory? category, int page = 1, int pageSize = 20) =>
        await assets.SearchAsync(search, status, category, page, pageSize);

    /// <summary>One asset with its recent loans and maintenance history.</summary>
    [HttpGet("{id:int}")]
    public async Task<AssetDetailDto> Get(int id) => await assets.GetAsync(id);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssetDto>> Create(AssetRequest request)
    {
        var asset = await assets.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = asset.Id }, asset);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<AssetDto> Update(int id, AssetRequest request) => await assets.UpdateAsync(id, request);

    /// <summary>Retire an asset. Assets are never deleted, so their history is kept.</summary>
    [HttpPost("{id:int}/retire")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Retire(int id)
    {
        await assets.RetireAsync(id);
        return NoContent();
    }

    /// <summary>Send an asset for repair (status becomes InRepair).</summary>
    [HttpPost("{id:int}/repairs")]
    [Authorize(Roles = "Admin")]
    public async Task<MaintenanceDto> SendToRepair(int id, RepairRequest request) =>
        await assets.SendToRepairAsync(id, request);

    /// <summary>Finish a repair (status goes back to Available).</summary>
    [HttpPost("repairs/{maintenanceId:int}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<MaintenanceDto> CompleteRepair(int maintenanceId, CompleteRepairRequest request) =>
        await assets.CompleteRepairAsync(maintenanceId, request);
}
