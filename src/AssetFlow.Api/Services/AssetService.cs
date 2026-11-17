using AssetFlow.Api.Contracts;
using AssetFlow.Api.Data;
using AssetFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Api.Services;

public interface IAssetService
{
    Task<PagedResult<AssetDto>> SearchAsync(string? search, AssetStatus? status, AssetCategory? category, int page, int pageSize);
    Task<AssetDetailDto> GetAsync(int id);
    Task<AssetDto> CreateAsync(AssetRequest request);
    Task<AssetDto> UpdateAsync(int id, AssetRequest request);
    Task RetireAsync(int id);
    Task<MaintenanceDto> SendToRepairAsync(int id, RepairRequest request);
    Task<MaintenanceDto> CompleteRepairAsync(int maintenanceId, CompleteRepairRequest request);
}

public class AssetService(AppDbContext db, TimeProvider clock) : IAssetService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<AssetDto>> SearchAsync(
        string? search, AssetStatus? status, AssetCategory? category, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Assets.AsNoTracking();
        if (status is not null) query = query.Where(a => a.Status == status);
        if (category is not null) query = query.Where(a => a.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            // "laptop" or "laptops" should also find every asset in the Laptop category
            var isCategory = Enum.TryParse<AssetCategory>(term.TrimEnd('s'), ignoreCase: true, out var searchCategory);

            query = query.Where(a => a.Tag.ToLower().Contains(term) || a.Name.ToLower().Contains(term)
                                     || (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term))
                                     || (isCategory && a.Category == searchCategory));
        }

        var total = await query.CountAsync();
        var items = await query.OrderBy(a => a.Tag).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<AssetDto>(items.Select(a => a.ToDto()).ToList(), page, pageSize, total);
    }

    public async Task<AssetDetailDto> GetAsync(int id)
    {
        var asset = await db.Assets.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id)
                    ?? throw new NotFoundException($"Asset {id} not found.");
        var loans = await db.Loans.AsNoTracking()
            .Include(l => l.Asset).Include(l => l.Borrower).Include(l => l.ReviewedBy)
            .Where(l => l.AssetId == id).OrderByDescending(l => l.RequestedAt).Take(10).ToListAsync();
        var maintenance = await db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.AssetId == id).OrderByDescending(m => m.OpenedAt).ToListAsync();

        return new AssetDetailDto(asset.ToDto(), loans.Select(l => l.ToDto(Now)).ToList(),
            maintenance.Select(m => m.ToDto()).ToList());
    }

    public async Task<AssetDto> CreateAsync(AssetRequest r)
    {
        await EnsureTagIsFree(r.Tag, exceptId: null);
        var asset = new Asset(r.Tag, r.Name, r.Category, r.SerialNumber, r.Location, r.PurchasedOn, Now);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        return asset.ToDto();
    }

    public async Task<AssetDto> UpdateAsync(int id, AssetRequest r)
    {
        var asset = await FindAsync(id);
        await EnsureTagIsFree(r.Tag, exceptId: id);
        asset.Update(r.Tag, r.Name, r.Category, r.SerialNumber, r.Location, r.PurchasedOn);
        await db.SaveChangesAsync();
        return asset.ToDto();
    }

    public async Task RetireAsync(int id)
    {
        var asset = await FindAsync(id);
        asset.Retire();
        await db.SaveChangesAsync();
    }

    public async Task<MaintenanceDto> SendToRepairAsync(int id, RepairRequest request)
    {
        var asset = await FindAsync(id);
        var record = asset.SendToRepair(request.Issue, Now);
        await db.SaveChangesAsync();
        return record.ToDto();
    }

    public async Task<MaintenanceDto> CompleteRepairAsync(int maintenanceId, CompleteRepairRequest request)
    {
        var record = await db.MaintenanceRecords.Include(m => m.Asset).SingleOrDefaultAsync(m => m.Id == maintenanceId)
                     ?? throw new NotFoundException($"Maintenance record {maintenanceId} not found.");
        record.Close(request.Resolution ?? "", request.Cost, Now);
        await db.SaveChangesAsync();
        return record.ToDto();
    }

    private async Task<Asset> FindAsync(int id) =>
        await db.Assets.SingleOrDefaultAsync(a => a.Id == id) ?? throw new NotFoundException($"Asset {id} not found.");

    private async Task EnsureTagIsFree(string tag, int? exceptId)
    {
        var normalized = tag.Trim().ToUpperInvariant();
        if (await db.Assets.AnyAsync(a => a.Tag == normalized && a.Id != exceptId))
            throw new ConflictException($"Tag {normalized} is already used.");
    }
}