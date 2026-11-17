using System.Net;
using System.Net.Http.Json;
using AssetFlow.Api.Contracts;
using AssetFlow.Api.Domain;

namespace AssetFlow.Tests;

/// <summary>End-to-end tests: real HTTP requests through controllers, services and the database.</summary>
public class ApiTests
{
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(TestJson.Options))!;
    }

    private static async Task<AssetDto> FindAsset(HttpClient client, string tag)
    {
        var page = await Read<PagedResult<AssetDto>>(await client.GetAsync($"/api/assets?search={tag}"));
        return Assert.Single(page.Items);
    }

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        using var app = new ApiFactory();
        var response = await app.CreateClient().GetAsync("/api/assets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_returns_401()
    {
        using var app = new ApiFactory();
        var response = await app.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@assetflow.dev", "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_creates_a_staff_account_and_rejects_duplicates()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();
        var request = new RegisterRequest("Aina Zulkifli", "Aina@Example.com", "Secret123!", "Finance");

        var auth = await Read<AuthResponse>(await client.PostAsJsonAsync("/api/auth/register", request));
        Assert.Equal(UserRole.Staff, auth.User.Role);
        Assert.Equal("aina@example.com", auth.User.Email);

        var again = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Staff_cannot_use_admin_endpoints()
    {
        using var app = new ApiFactory();
        var staff = await app.StaffAsync();

        var create = await staff.PostAsJsonAsync("/api/assets",
            new AssetRequest("LAP-9999", "Sneaky laptop", AssetCategory.Laptop, null, null, null));
        var report = await staff.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_search_and_update_assets()
    {
        using var app = new ApiFactory();
        var admin = await app.AdminAsync();

        var created = await Read<AssetDto>(await admin.PostAsJsonAsync("/api/assets",
            new AssetRequest("prj-0002", "BenQ Projector", AssetCategory.Projector, "BQ-1", "Room B", null)));
        Assert.Equal("PRJ-0002", created.Tag);

        var duplicate = await admin.PostAsJsonAsync("/api/assets",
            new AssetRequest("PRJ-0002", "Copy", AssetCategory.Projector, null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var projectors = await Read<PagedResult<AssetDto>>(await admin.GetAsync("/api/assets?category=Projector"));
        Assert.Equal(2, projectors.TotalCount);

        var updated = await Read<AssetDto>(await admin.PutAsJsonAsync($"/api/assets/{created.Id}",
            new AssetRequest("PRJ-0002", "BenQ Projector", AssetCategory.Projector, "BQ-1", "Room C", null)));
        Assert.Equal("Room C", updated.Location);
    }

    [Theory]
    [InlineData("laptop", null, 4)]            // matches the category, not just names
    [InlineData("Laptops", "available", 4)]   // plural and lowercase status both work
    [InlineData("dell", null, 4)]             // 2 laptops + 2 monitors
    [InlineData("LAP-0003", null, 1)]
    public async Task Search_matches_tag_name_serial_and_category(string search, string? status, int expected)
    {
        using var app = new ApiFactory();
        var staff = await app.StaffAsync();
        var url = $"/api/assets?search={search}" + (status is null ? "" : $"&status={status}");
        var page = await Read<PagedResult<AssetDto>>(await staff.GetAsync(url));
        Assert.Equal(expected, page.TotalCount);
    }

    [Fact]
    public async Task Full_loan_flow_from_request_to_return()
    {
        using var app = new ApiFactory();
        var staff = await app.StaffAsync();
        var admin = await app.AdminAsync();
        var laptop = await FindAsset(staff, "LAP-0001");

        var loan = await Read<LoanDto>(await staff.PostAsJsonAsync("/api/loans",
            new LoanRequest(laptop.Id, app.Today, app.Today.AddDays(4), "Client visit")));
        Assert.Equal(LoanStatus.Requested, loan.Status);

        loan = await Read<LoanDto>(await admin.PostAsJsonAsync($"/api/loans/{loan.Id}/approve", new ReviewRequest("OK")));
        Assert.Equal("Nadia Rahman", loan.ReviewedBy);

        loan = await Read<LoanDto>(await admin.PostAsync($"/api/loans/{loan.Id}/checkout", null));
        Assert.Equal(LoanStatus.CheckedOut, loan.Status);
        Assert.Equal(AssetStatus.OnLoan, (await FindAsset(admin, "LAP-0001")).Status);

        loan = await Read<LoanDto>(await admin.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new ReturnRequest("Good")));
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(AssetStatus.Available, (await FindAsset(admin, "LAP-0001")).Status);
    }

    [Fact]
    public async Task The_same_asset_cannot_be_booked_twice_for_overlapping_dates()
    {
        using var app = new ApiFactory();
        var staff = await app.StaffAsync();
        var laptop = await FindAsset(staff, "LAP-0002");

        await Read<LoanDto>(await staff.PostAsJsonAsync("/api/loans",
            new LoanRequest(laptop.Id, app.Today.AddDays(2), app.Today.AddDays(5), "Workshop")));

        var overlapping = await staff.PostAsJsonAsync("/api/loans",
            new LoanRequest(laptop.Id, app.Today.AddDays(4), app.Today.AddDays(8), "Another workshop"));
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);

        var afterwards = await staff.PostAsJsonAsync("/api/loans",
            new LoanRequest(laptop.Id, app.Today.AddDays(6), app.Today.AddDays(8), "Later workshop"));
        Assert.Equal(HttpStatusCode.Created, afterwards.StatusCode);
    }

    [Fact]
    public async Task Invalid_steps_return_a_clear_400()
    {
        using var app = new ApiFactory();
        var staff = await app.StaffAsync();
        var admin = await app.AdminAsync();
        var laptop = await FindAsset(staff, "LAP-0003");

        var loan = await Read<LoanDto>(await staff.PostAsJsonAsync("/api/loans",
            new LoanRequest(laptop.Id, app.Today, app.Today, "Demo")));

        var response = await admin.PostAsync($"/api/loans/{loan.Id}/checkout", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Only a loan that is Approved", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Staff_only_see_and_cancel_their_own_loans()
    {
        using var app = new ApiFactory();
        var daniel = await app.StaffAsync();
        var laptop = await FindAsset(daniel, "LAP-0004");
        var loan = await Read<LoanDto>(await daniel.PostAsJsonAsync("/api/loans",
            new LoanRequest(laptop.Id, app.Today, app.Today.AddDays(1), "Training")));

        // A second staff member signs up
        var other = app.CreateClient();
        var auth = await Read<AuthResponse>(await other.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Other Person", "other@assetflow.dev", "Secret123!", null)));
        other.DefaultRequestHeaders.Authorization = new("Bearer", auth.Token);

        Assert.Empty(await Read<List<LoanDto>>(await other.GetAsync("/api/loans")));
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/loans/{loan.Id}/cancel", null)).StatusCode);

        var cancelled = await Read<LoanDto>(await daniel.PostAsync($"/api/loans/{loan.Id}/cancel", null));
        Assert.Equal(LoanStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task Overdue_loans_show_up_after_the_due_date_and_in_the_report()
    {
        using var app = new ApiFactory();
        var staff = await app.StaffAsync();
        var admin = await app.AdminAsync();
        var camera = await FindAsset(staff, "CAM-0001");

        var loan = await Read<LoanDto>(await staff.PostAsJsonAsync("/api/loans",
            new LoanRequest(camera.Id, app.Today, app.Today.AddDays(1), "Product shoot")));
        await Read<LoanDto>(await admin.PostAsJsonAsync($"/api/loans/{loan.Id}/approve", new ReviewRequest(null)));
        await Read<LoanDto>(await admin.PostAsync($"/api/loans/{loan.Id}/checkout", null));

        Assert.Empty(await Read<List<LoanDto>>(await admin.GetAsync("/api/loans?overdue=true")));

        app.Clock.Advance(TimeSpan.FromDays(3)); // time passes, the camera isn't back

        var overdue = Assert.Single(await Read<List<LoanDto>>(await admin.GetAsync("/api/loans?overdue=true")));
        Assert.True(overdue.IsOverdue);

        var report = await Read<SummaryReport>(await admin.GetAsync("/api/reports/summary"));
        Assert.Equal(1, report.OverdueLoans);
        Assert.Equal(1, report.ActiveLoans);
        Assert.Contains(report.AssetsByStatus, s => s.Status == "OnLoan" && s.Count == 1);
        Assert.Equal("CAM-0001", Assert.Single(report.MostBorrowed).Tag);
    }

    [Fact]
    public async Task Repair_flow_blocks_loans_and_records_cost()
    {
        using var app = new ApiFactory();
        var admin = await app.AdminAsync();
        var monitor = await FindAsset(admin, "MON-0001");

        var repair = await Read<MaintenanceDto>(await admin.PostAsJsonAsync(
            $"/api/assets/{monitor.Id}/repairs", new RepairRequest("Dead pixels")));

        var loanWhileBroken = await admin.PostAsJsonAsync("/api/loans",
            new LoanRequest(monitor.Id, app.Today, app.Today, "Need a screen"));
        Assert.Equal(HttpStatusCode.Conflict, loanWhileBroken.StatusCode);

        await Read<MaintenanceDto>(await admin.PostAsJsonAsync(
            $"/api/assets/repairs/{repair.Id}/complete", new CompleteRepairRequest("Panel replaced", 420m)));

        var detail = await Read<AssetDetailDto>(await admin.GetAsync($"/api/assets/{monitor.Id}"));
        Assert.Equal(AssetStatus.Available, detail.Asset.Status);
        Assert.Equal(420m, Assert.Single(detail.Maintenance).Cost);

        var report = await Read<SummaryReport>(await admin.GetAsync("/api/reports/summary"));
        Assert.Equal(420m, report.RepairCostThisYear);
    }
}