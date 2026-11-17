namespace AssetFlow.Api.Domain;

/// <summary>
/// A request to borrow an asset. Each method moves the loan one step through its
/// lifecycle and refuses steps that are not allowed from the current status.
/// </summary>
public class Loan
{
    public const int MaxLoanDays = 30;

    public int Id { get; private set; }
    public int AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public int BorrowerId { get; private set; }
    public User Borrower { get; private set; } = null!;
    public string Purpose { get; private set; } = "";
    public DateOnly StartDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public LoanStatus Status { get; private set; } = LoanStatus.Requested;

    public DateTime RequestedAt { get; private set; }
    public int? ReviewedById { get; private set; }
    public User? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime? CheckedOutAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public string? ReturnCondition { get; private set; }

    private Loan() { } // for EF Core

    public Loan(Asset asset, User borrower, DateOnly startDate, DateOnly dueDate, string purpose, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        if (!asset.CanBeBorrowed) throw new ConflictException($"{asset.Tag} is {asset.Status} and cannot be borrowed.");
        if (startDate < today) throw new DomainException("Start date cannot be in the past.");
        if (dueDate < startDate) throw new DomainException("Due date must be on or after the start date.");
        if (dueDate.DayNumber - startDate.DayNumber + 1 > MaxLoanDays)
            throw new DomainException($"Loans can be at most {MaxLoanDays} days.");
        if (string.IsNullOrWhiteSpace(purpose)) throw new DomainException("Purpose is required.");

        Asset = asset;
        AssetId = asset.Id;
        Borrower = borrower;
        BorrowerId = borrower.Id;
        StartDate = startDate;
        DueDate = dueDate;
        Purpose = purpose.Trim();
        RequestedAt = now;
    }

    /// <summary>Does this loan block the asset between the given dates?</summary>
    public bool Overlaps(DateOnly start, DateOnly end) =>
        Status is LoanStatus.Requested or LoanStatus.Approved or LoanStatus.CheckedOut
        && start <= DueDate && end >= StartDate;

    public bool IsOverdue(DateTime now) => Status == LoanStatus.CheckedOut && DateOnly.FromDateTime(now) > DueDate;

    public void Approve(User admin, string? note, DateTime now)
    {
        EnsureAdmin(admin);
        EnsureStatus(LoanStatus.Requested, "approved");
        Status = LoanStatus.Approved;
        Review(admin, note, now);
    }

    public void Reject(User admin, string? note, DateTime now)
    {
        EnsureAdmin(admin);
        EnsureStatus(LoanStatus.Requested, "rejected");
        if (string.IsNullOrWhiteSpace(note)) throw new DomainException("Give a reason when rejecting a loan.");
        Status = LoanStatus.Rejected;
        Review(admin, note, now);
    }

    public void Cancel(User user)
    {
        if (user.Id != BorrowerId && !user.IsAdmin) throw new DomainException("Only the borrower can cancel this loan.");
        if (Status is not (LoanStatus.Requested or LoanStatus.Approved))
            throw new DomainException($"A {Status} loan cannot be cancelled.");
        Status = LoanStatus.Cancelled;
    }

    public void CheckOut(DateTime now)
    {
        EnsureStatus(LoanStatus.Approved, "checked out");
        if (DateOnly.FromDateTime(now) < StartDate)
            throw new DomainException($"This loan starts on {StartDate:yyyy-MM-dd}.");
        Asset.MarkOnLoan();
        Status = LoanStatus.CheckedOut;
        CheckedOutAt = now;
    }

    public void Return(string? condition, DateTime now)
    {
        EnsureStatus(LoanStatus.CheckedOut, "returned");
        Asset.MarkReturned();
        Status = LoanStatus.Returned;
        ReturnedAt = now;
        ReturnCondition = string.IsNullOrWhiteSpace(condition) ? null : condition.Trim();
    }

    private void Review(User admin, string? note, DateTime now)
    {
        ReviewedBy = admin;
        ReviewedById = admin.Id;
        ReviewedAt = now;
        ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private void EnsureStatus(LoanStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Only a loan that is {expected} can be {action}. This loan is {Status}.");
    }

    private static void EnsureAdmin(User user)
    {
        if (!user.IsAdmin) throw new DomainException("Only an admin can review loans.");
    }
}
