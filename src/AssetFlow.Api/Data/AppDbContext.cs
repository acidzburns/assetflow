using AssetFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Store enums as readable text ("OnLoan") instead of numbers (1)
        builder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<AssetStatus>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<AssetCategory>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<LoanStatus>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.Property(u => u.Name).HasMaxLength(100);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.Department).HasMaxLength(100);
            e.HasIndex(u => u.Email).IsUnique();
        });

        model.Entity<Asset>(e =>
        {
            e.Property(a => a.Tag).HasMaxLength(20);
            e.Property(a => a.Name).HasMaxLength(150);
            e.Property(a => a.SerialNumber).HasMaxLength(100);
            e.Property(a => a.Location).HasMaxLength(100);
            e.HasIndex(a => a.Tag).IsUnique();
            e.HasIndex(a => a.Status);
        });

        model.Entity<Loan>(e =>
        {
            e.Property(l => l.Purpose).HasMaxLength(300);
            e.Property(l => l.ReviewNote).HasMaxLength(300);
            e.Property(l => l.ReturnCondition).HasMaxLength(300);
            e.HasOne(l => l.Asset).WithMany(a => a.Loans).HasForeignKey(l => l.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Borrower).WithMany().HasForeignKey(l => l.BorrowerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ReviewedBy).WithMany().HasForeignKey(l => l.ReviewedById).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => new { l.AssetId, l.Status });
            e.HasIndex(l => new { l.BorrowerId, l.Status });
        });

        model.Entity<MaintenanceRecord>(e =>
        {
            e.Property(m => m.Issue).HasMaxLength(300);
            e.Property(m => m.Resolution).HasMaxLength(300);
            e.Property(m => m.Cost).HasPrecision(10, 2);
            e.HasOne(m => m.Asset).WithMany(a => a.MaintenanceRecords).HasForeignKey(m => m.AssetId);
        });
    }
}
