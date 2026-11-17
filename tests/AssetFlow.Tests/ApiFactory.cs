using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssetFlow.Api.Contracts;
using AssetFlow.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace AssetFlow.Tests;

/// <summary>
/// Starts the real API in memory for tests. Two things are swapped:
///  - the database: a throwaway SQLite file instead of PostgreSQL
///  - the clock: a fake clock we can move forward (to test overdue loans)
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"assetflow-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", "Host=not-used-in-tests");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public DateOnly Today => DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);

    /// <summary>An HTTP client already logged in as the given user.</summary>
    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    public Task<HttpClient> AdminAsync() => LoginAsync("admin@assetflow.dev", "Admin@12345");
    public Task<HttpClient> StaffAsync() => LoginAsync("staff@assetflow.dev", "Staff@12345");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

public static class TestJson
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };
}
