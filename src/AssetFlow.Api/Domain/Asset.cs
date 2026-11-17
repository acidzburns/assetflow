namespace AssetFlow.Api.Domain;

/// <summary>
/// A piece of company equipment. The status can only change through the methods
/// below, so the asset can never end up in an impossible state
/// (for example "on loan" and "in repair" at the same time).
/// </summary>
public class Asset
{
    public int Id { get; private set; }
    public string Tag { get; private set; } = "";          // e.g. LAP-0001, printed on the QR label
    public string Name { get; private set; } = "";
    public AssetCategory Category { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? Location { get; private set; }
    public DateOnly? PurchasedOn { get; private set; }
    public AssetStatus Status { get; private set; } = AssetStatus.Available;
    public DateTime CreatedAt { get; private set; }

    public List<Loan> Loans { get; private set; } = [];
    public List<MaintenanceRecord> MaintenanceRecords { get; private set; } = [];

    private Asset() { } // for EF Core

    public Asset(string tag, string name, AssetCategory category, string? serialNumber, string? location,
        DateOnly? purchasedOn, DateTime createdAt)
    {
        CreatedAt = createdAt;
        Update(tag, name, category, serialNumber, location, purchasedOn);
    }

    public void Update(string tag, string name, AssetCategory category, string? serialNumber, string? location,
        DateOnly? purchasedOn)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new DomainException("Tag is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");

        Tag = tag.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        SerialNumber = serialNumber?.Trim();
        Location = location?.Trim();
        PurchasedOn = purchasedOn;
    }

    public bool CanBeBorrowed => Status is AssetStatus.Available or AssetStatus.OnLoan;

    public void MarkOnLoan()
    {
        if (Status != AssetStatus.Available)
            throw new ConflictException($"{Tag} cannot be checked out because it is {Status}.");
        Status = AssetStatus.OnLoan;
    }

    public void MarkReturned()
    {
        if (Status != AssetStatus.OnLoan)
            throw new DomainException($"{Tag} is not on loan.");
        Status = AssetStatus.Available;
    }

    public MaintenanceRecord SendToRepair(string issue, DateTime now)
    {
        if (Status == AssetStatus.OnLoan) throw new ConflictException($"{Tag} is on loan. Return it before repair.");
        if (Status == AssetStatus.Retired) throw new DomainException($"{Tag} is retired.");
        if (Status == AssetStatus.InRepair) throw new ConflictException($"{Tag} is already in repair.");

        Status = AssetStatus.InRepair;
        var record = new MaintenanceRecord(this, issue, now);
        MaintenanceRecords.Add(record);
        return record;
    }

    public void CompleteRepair()
    {
        if (Status != AssetStatus.InRepair) throw new DomainException($"{Tag} is not in repair.");
        Status = AssetStatus.Available;
    }

    public void Retire()
    {
        if (Status == AssetStatus.OnLoan) throw new ConflictException($"{Tag} is on loan. Return it before retiring.");
        Status = AssetStatus.Retired;
    }
}
