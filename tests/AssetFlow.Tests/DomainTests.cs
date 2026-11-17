using AssetFlow.Api.Domain;

namespace AssetFlow.Tests;

/// <summary>Business rules, tested on plain C# objects (no database, no web server).</summary>
public class DomainTests
{
    private static readonly DateTime Now = new(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private static Asset Laptop() =>
        new("lap-0099", "Test Laptop", AssetCategory.Laptop, null, null, null, Now);

    private static User Staff() => new("Staff Member", "staff@test.dev", UserRole.Staff, null, Now);
    private static User Admin() => new("Admin", "admin@test.dev", UserRole.Admin, null, Now);

    private static Loan NewLoan(Asset? asset = null, int days = 3) =>
        new(asset ?? Laptop(), Staff(), Today, Today.AddDays(days - 1), "Testing", Now);

    [Fact]
    public void Asset_tag_is_normalised_to_upper_case()
    {
        Assert.Equal("LAP-0099", Laptop().Tag);
    }

    [Fact]
    public void Full_loan_lifecycle_moves_asset_out_and_back()
    {
        var asset = Laptop();
        var loan = NewLoan(asset);

        loan.Approve(Admin(), "ok", Now);
        loan.CheckOut(Now);
        Assert.Equal(LoanStatus.CheckedOut, loan.Status);
        Assert.Equal(AssetStatus.OnLoan, asset.Status);

        loan.Return("Good", Now.AddDays(2));
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.Equal("Good", loan.ReturnCondition);
    }

    [Fact]
    public void Cannot_check_out_a_loan_that_is_not_approved()
    {
        var loan = NewLoan();
        var error = Assert.Throws<DomainException>(() => loan.CheckOut(Now));
        Assert.Contains("Approved", error.Message);
    }

    [Fact]
    public void Staff_cannot_approve_loans()
    {
        var loan = NewLoan();
        Assert.Throws<DomainException>(() => loan.Approve(Staff(), null, Now));
    }

    [Fact]
    public void Rejecting_requires_a_reason()
    {
        var loan = NewLoan();
        Assert.Throws<DomainException>(() => loan.Reject(Admin(), "  ", Now));
        loan.Reject(Admin(), "Not available for external events", Now);
        Assert.Equal(LoanStatus.Rejected, loan.Status);
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void Loans_are_limited_to_30_days(int days, bool allowed)
    {
        if (allowed) Assert.NotNull(NewLoan(days: days));
        else Assert.Throws<DomainException>(() => NewLoan(days: days));
    }

    [Fact]
    public void Start_date_cannot_be_in_the_past()
    {
        Assert.Throws<DomainException>(() =>
            new Loan(Laptop(), Staff(), Today.AddDays(-1), Today.AddDays(2), "x", Now));
    }

    [Fact]
    public void Loan_is_overdue_only_after_the_due_date_while_checked_out()
    {
        var loan = NewLoan(days: 2); // due tomorrow
        loan.Approve(Admin(), null, Now);
        loan.CheckOut(Now);

        Assert.False(loan.IsOverdue(Now.AddDays(1)));
        Assert.True(loan.IsOverdue(Now.AddDays(2)));

        loan.Return(null, Now.AddDays(3));
        Assert.False(loan.IsOverdue(Now.AddDays(3)));
    }

    [Fact]
    public void Retired_or_repaired_assets_cannot_be_borrowed()
    {
        var retired = Laptop();
        retired.Retire();
        Assert.Throws<ConflictException>(() => NewLoan(retired));

        var broken = Laptop();
        broken.SendToRepair("Screen cracked", Now);
        Assert.Throws<ConflictException>(() => NewLoan(broken));
    }

    [Fact]
    public void Asset_on_loan_cannot_be_sent_to_repair_or_retired()
    {
        var asset = Laptop();
        var loan = NewLoan(asset);
        loan.Approve(Admin(), null, Now);
        loan.CheckOut(Now);

        Assert.Throws<ConflictException>(() => asset.SendToRepair("noise", Now));
        Assert.Throws<ConflictException>(() => asset.Retire());
    }

    [Fact]
    public void Completing_a_repair_makes_the_asset_available_again()
    {
        var asset = Laptop();
        var record = asset.SendToRepair("Battery swelling", Now);
        record.Close("Battery replaced", 350.50m, Now.AddDays(3));

        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.False(record.IsOpen);
        Assert.Throws<DomainException>(() => record.Close("again", null, Now));
    }
}
