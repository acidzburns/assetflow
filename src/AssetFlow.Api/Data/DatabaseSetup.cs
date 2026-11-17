using AssetFlow.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Api.Data;

public static class DatabaseSetup
{
    /// <summary>Create/upgrade the database and add demo data the first time.</summary>
    public static async Task InitialiseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        if (db.Database.IsNpgsql()) await db.Database.MigrateAsync(); // PostgreSQL: apply migrations
        else await db.Database.EnsureCreatedAsync();                    // tests (SQLite)

        if (!config.GetValue("Seed:Enabled", true) || await db.Users.AnyAsync()) return;

        var admin = new User("Nadia Rahman", "admin@assetflow.dev", UserRole.Admin, "IT Operations", now);
        admin.SetPasswordHash(hasher.HashPassword(admin, config["Seed:AdminPassword"] ?? "Admin@12345"));
        var staff = new User("Daniel Lim", "staff@assetflow.dev", UserRole.Staff, "Marketing", now);
        staff.SetPasswordHash(hasher.HashPassword(staff, config["Seed:StaffPassword"] ?? "Staff@12345"));
        db.Users.AddRange(admin, staff);

        var bought = new DateOnly(2025, 3, 1);
        db.Assets.AddRange(
            new Asset("LAP-0001", "Dell Latitude 7450", AssetCategory.Laptop, "DL7450-88123", "IT Store, Level 3", bought, now),
            new Asset("LAP-0002", "Dell Latitude 7450", AssetCategory.Laptop, "DL7450-88124", "IT Store, Level 3", bought, now),
            new Asset("LAP-0003", "MacBook Air 13\" M3", AssetCategory.Laptop, "MBA13-55091", "IT Store, Level 3", bought, now),
            new Asset("LAP-0004", "Lenovo ThinkPad T14", AssetCategory.Laptop, "LT14-20931", "IT Store, Level 3", bought, now),
            new Asset("MON-0001", "Dell UltraSharp 27\"", AssetCategory.Monitor, "U2724-1192", "IT Store, Level 3", bought, now),
            new Asset("MON-0002", "Dell UltraSharp 27\"", AssetCategory.Monitor, "U2724-1193", "IT Store, Level 3", bought, now),
            new Asset("TAB-0001", "iPad 10th Gen", AssetCategory.Tablet, "IPAD10-7731", "Reception", bought, now),
            new Asset("PRJ-0001", "Epson EB-FH52 Projector", AssetCategory.Projector, "EBFH52-3310", "Meeting Room A", bought, now),
            new Asset("CAM-0001", "Sony ZV-E10 Camera", AssetCategory.Camera, "ZVE10-6620", "Marketing, Level 2", bought, now),
            new Asset("ACC-0001", "Logitech MX Keys Keyboard", AssetCategory.Accessory, null, "IT Store, Level 3", bought, now),
            new Asset("ACC-0002", "USB-C Docking Station", AssetCategory.Accessory, "DOCK-4410", "IT Store, Level 3", bought, now),
            new Asset("ACC-0003", "Wireless Presenter", AssetCategory.Accessory, null, "Meeting Room A", bought, now));

        await db.SaveChangesAsync();
    }
}
