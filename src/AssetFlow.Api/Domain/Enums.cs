namespace AssetFlow.Api.Domain;

public enum UserRole
{
    Staff,
    Admin,
}

public enum AssetStatus
{
    Available,
    OnLoan,
    InRepair,
    Retired,
}

public enum AssetCategory
{
    Laptop,
    Monitor,
    Tablet,
    Projector,
    Camera,
    Accessory,
}

/// <summary>
/// Requested → Approved → CheckedOut → Returned
///     ↘ Rejected   ↘ Cancelled (by the borrower, before check-out)
/// </summary>
public enum LoanStatus
{
    Requested,
    Approved,
    Rejected,
    Cancelled,
    CheckedOut,
    Returned,
}
