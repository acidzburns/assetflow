using AssetFlow.Api.Contracts;
using AssetFlow.Api.Domain;

namespace AssetFlow.Api.Services;

/// <summary>Turns domain objects into the DTOs the API returns.</summary>
public static class Mapping
{
    public static UserDto ToDto(this User u) => new(u.Id, u.Name, u.Email, u.Role, u.Department);

    public static AssetDto ToDto(this Asset a) =>
        new(a.Id, a.Tag, a.Name, a.Category, a.SerialNumber, a.Location, a.PurchasedOn, a.Status);

    public static LoanDto ToDto(this Loan l, DateTime now) => new(
        l.Id, l.AssetId, l.Asset.Tag, l.Asset.Name, l.BorrowerId, l.Borrower.Name, l.Purpose,
        l.StartDate, l.DueDate, l.Status, l.IsOverdue(now), l.RequestedAt,
        l.ReviewedBy?.Name, l.ReviewNote, l.CheckedOutAt, l.ReturnedAt);

    public static MaintenanceDto ToDto(this MaintenanceRecord m) =>
        new(m.Id, m.AssetId, m.Issue, m.Resolution, m.Cost, m.OpenedAt, m.ClosedAt);
}
