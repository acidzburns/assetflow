namespace AssetFlow.Api.Domain;

public class MaintenanceRecord
{
    public int Id { get; private set; }
    public int AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public string Issue { get; private set; } = "";
    public string? Resolution { get; private set; }
    public decimal? Cost { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public bool IsOpen => ClosedAt is null;

    private MaintenanceRecord() { } // for EF Core

    internal MaintenanceRecord(Asset asset, string issue, DateTime openedAt)
    {
        if (string.IsNullOrWhiteSpace(issue)) throw new DomainException("Describe the issue.");
        Asset = asset;
        Issue = issue.Trim();
        OpenedAt = openedAt;
    }

    public void Close(string resolution, decimal? cost, DateTime now)
    {
        if (!IsOpen) throw new DomainException("This maintenance record is already closed.");
        if (cost < 0) throw new DomainException("Cost cannot be negative.");
        Resolution = string.IsNullOrWhiteSpace(resolution) ? null : resolution.Trim();
        Cost = cost;
        ClosedAt = now;
        Asset.CompleteRepair();
    }
}
