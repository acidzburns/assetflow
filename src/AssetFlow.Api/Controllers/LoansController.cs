using AssetFlow.Api.Contracts;
using AssetFlow.Api.Domain;
using AssetFlow.Api.Infrastructure;
using AssetFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController(ILoanService loans) : ControllerBase
{
    /// <summary>Staff see their own loans; admins see everyone's.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<LoanDto>> List(LoanStatus? status, bool overdue = false) =>
        await loans.ListAsync(User.UserId(), User.IsAdmin(), status, overdue);

    [HttpGet("{id:int}")]
    public async Task<LoanDto> Get(int id) => await loans.GetAsync(id, User.UserId(), User.IsAdmin());

    /// <summary>Request to borrow an asset.</summary>
    [HttpPost]
    public async Task<ActionResult<LoanDto>> Create(LoanRequest request)
    {
        var loan = await loans.RequestAsync(User.UserId(), request);
        return CreatedAtAction(nameof(Get), new { id = loan.Id }, loan);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<LoanDto> Cancel(int id) => await loans.CancelAsync(id, User.UserId());

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<LoanDto> Approve(int id, ReviewRequest request) =>
        await loans.ApproveAsync(id, User.UserId(), request);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<LoanDto> Reject(int id, ReviewRequest request) =>
        await loans.RejectAsync(id, User.UserId(), request);

    /// <summary>Hand the asset to the borrower.</summary>
    [HttpPost("{id:int}/checkout")]
    [Authorize(Roles = "Admin")]
    public async Task<LoanDto> CheckOut(int id) => await loans.CheckOutAsync(id);

    /// <summary>Receive the asset back.</summary>
    [HttpPost("{id:int}/return")]
    [Authorize(Roles = "Admin")]
    public async Task<LoanDto> Return(int id, ReturnRequest request) => await loans.ReturnAsync(id, request);
}
