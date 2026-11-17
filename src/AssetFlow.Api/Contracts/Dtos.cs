using System.ComponentModel.DataAnnotations;
using AssetFlow.Api.Domain;

namespace AssetFlow.Api.Contracts;

// ── Auth ──────────────────────────────────────────────────────────────────
public record RegisterRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [StringLength(100)] string? Department);

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public record UserDto(int Id, string Name, string Email, UserRole Role, string? Department);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

// ── Assets ────────────────────────────────────────────────────────────────
public record AssetRequest(
    [Required, StringLength(20)] string Tag,
    [Required, StringLength(150)] string Name,
    AssetCategory Category,
    [StringLength(100)] string? SerialNumber,
    [StringLength(100)] string? Location,
    DateOnly? PurchasedOn);

public record AssetDto(
    int Id, string Tag, string Name, AssetCategory Category, string? SerialNumber,
    string? Location, DateOnly? PurchasedOn, AssetStatus Status);

public record AssetDetailDto(AssetDto Asset, IReadOnlyList<LoanDto> RecentLoans, IReadOnlyList<MaintenanceDto> Maintenance);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// ── Loans ─────────────────────────────────────────────────────────────────
public record LoanRequest(int AssetId, DateOnly StartDate, DateOnly DueDate, [Required, StringLength(300)] string Purpose);

public record ReviewRequest([StringLength(300)] string? Note);

public record ReturnRequest([StringLength(300)] string? Condition);

public record LoanDto(
    int Id, int AssetId, string AssetTag, string AssetName, int BorrowerId, string BorrowerName,
    string Purpose, DateOnly StartDate, DateOnly DueDate, LoanStatus Status, bool IsOverdue,
    DateTime RequestedAt, string? ReviewedBy, string? ReviewNote, DateTime? CheckedOutAt, DateTime? ReturnedAt);

// ── Maintenance ───────────────────────────────────────────────────────────
public record RepairRequest([Required, StringLength(300)] string Issue);

public record CompleteRepairRequest([StringLength(300)] string? Resolution, [Range(0, 1_000_000)] decimal? Cost);

public record MaintenanceDto(int Id, int AssetId, string Issue, string? Resolution, decimal? Cost, DateTime OpenedAt, DateTime? ClosedAt);

// ── Reports ───────────────────────────────────────────────────────────────
public record StatusCount(string Status, long Count);

public record TopAsset(string Tag, string Name, long LoanCount);

public record SummaryReport(
    IReadOnlyList<StatusCount> AssetsByStatus,
    int PendingRequests,
    int ActiveLoans,
    int OverdueLoans,
    IReadOnlyList<TopAsset> MostBorrowed,
    decimal RepairCostThisYear);
