using AssetFlow.Api.Contracts;
using AssetFlow.Api.Data;
using AssetFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Api.Services;

public interface ILoanService
{
    Task<IReadOnlyList<LoanDto>> ListAsync(int userId, bool isAdmin, LoanStatus? status, bool overdueOnly);
    Task<LoanDto> GetAsync(int id, int userId, bool isAdmin);
    Task<LoanDto> RequestAsync(int userId, LoanRequest request);
    Task<LoanDto> ApproveAsync(int id, int adminId, ReviewRequest request);
    Task<LoanDto> RejectAsync(int id, int adminId, ReviewRequest request);
    Task<LoanDto> CancelAsync(int id, int userId);
    Task<LoanDto> CheckOutAsync(int id);
    Task<LoanDto> ReturnAsync(int id, ReturnRequest request);
}

public class LoanService(AppDbContext db, TimeProvider clock) : ILoanService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private IQueryable<Loan> Loans =>
        db.Loans.Include(l => l.Asset).Include(l => l.Borrower).Include(l => l.ReviewedBy);

    public async Task<IReadOnlyList<LoanDto>> ListAsync(int userId, bool isAdmin, LoanStatus? status, bool overdueOnly)
    {
        var query = Loans.AsNoTracking();
        if (!isAdmin) query = query.Where(l => l.BorrowerId == userId); // staff only see their own loans
        if (status is not null) query = query.Where(l => l.Status == status);
        if (overdueOnly)
        {
            var today = DateOnly.FromDateTime(Now);
            query = query.Where(l => l.Status == LoanStatus.CheckedOut && l.DueDate < today);
        }

        var loans = await query.OrderByDescending(l => l.RequestedAt).Take(200).ToListAsync();
        return loans.Select(l => l.ToDto(Now)).ToList();
    }

    public async Task<LoanDto> GetAsync(int id, int userId, bool isAdmin)
    {
        var loan = await FindAsync(id);
        if (!isAdmin && loan.BorrowerId != userId) throw new NotFoundException($"Loan {id} not found.");
        return loan.ToDto(Now);
    }

    public async Task<LoanDto> RequestAsync(int userId, LoanRequest r)
    {
        var borrower = await db.Users.SingleAsync(u => u.Id == userId);
        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == r.AssetId)
                    ?? throw new NotFoundException($"Asset {r.AssetId} not found.");

        // An asset can't be booked twice for the same days
        var clash = await db.Loans.AnyAsync(l =>
            l.AssetId == asset.Id
            && (l.Status == LoanStatus.Requested || l.Status == LoanStatus.Approved || l.Status == LoanStatus.CheckedOut)
            && r.StartDate <= l.DueDate && r.DueDate >= l.StartDate);
        if (clash) throw new ConflictException($"{asset.Tag} is already booked for some of those dates.");

        var loan = new Loan(asset, borrower, r.StartDate, r.DueDate, r.Purpose, Now);
        db.Loans.Add(loan);
        await db.SaveChangesAsync();
        return loan.ToDto(Now);
    }

    public Task<LoanDto> ApproveAsync(int id, int adminId, ReviewRequest r) =>
        ChangeAsync(id, async loan => loan.Approve(await UserAsync(adminId), r.Note, Now));

    public Task<LoanDto> RejectAsync(int id, int adminId, ReviewRequest r) =>
        ChangeAsync(id, async loan => loan.Reject(await UserAsync(adminId), r.Note, Now));

    public async Task<LoanDto> CancelAsync(int id, int userId)
    {
        var user = await UserAsync(userId);
        var loan = await FindAsync(id);
        if (!user.IsAdmin && loan.BorrowerId != userId) throw new NotFoundException($"Loan {id} not found.");
        loan.Cancel(user);
        await db.SaveChangesAsync();
        return loan.ToDto(Now);
    }

    public Task<LoanDto> CheckOutAsync(int id) => ChangeAsync(id, loan => { loan.CheckOut(Now); return Task.CompletedTask; });

    public Task<LoanDto> ReturnAsync(int id, ReturnRequest r) =>
        ChangeAsync(id, loan => { loan.Return(r.Condition, Now); return Task.CompletedTask; });

    private async Task<LoanDto> ChangeAsync(int id, Func<Loan, Task> change)
    {
        var loan = await FindAsync(id);
        await change(loan);
        await db.SaveChangesAsync();
        return loan.ToDto(Now);
    }

    private async Task<Loan> FindAsync(int id) =>
        await Loans.SingleOrDefaultAsync(l => l.Id == id) ?? throw new NotFoundException($"Loan {id} not found.");

    private async Task<User> UserAsync(int id) => await db.Users.SingleAsync(u => u.Id == id);
}
