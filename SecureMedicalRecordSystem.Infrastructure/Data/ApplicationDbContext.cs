using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Infrastructure.Data;

/// <summary>
/// EF Core database context for the Secure Medical Record System.
/// All PII fields are encrypted before persistence via AES-256.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalFile> MedicalFiles => Set<MedicalFile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter: exclude soft-deleted records for all BaseEntity types
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalFile>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);

        // AuditLog — fully immutable, never soft-deleted
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
        });

        // Encrypted field column sizes
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.Property(e => e.EncryptedDiagnosis).HasMaxLength(8000);
            entity.Property(e => e.EncryptedNotes).HasMaxLength(8000);
            entity.Property(e => e.EncryptedPrescriptions).HasMaxLength(8000);
            entity.Property(e => e.EncryptedLabResults).HasMaxLength(8000);
        });

        // User → RefreshTokens
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Patient → MedicalRecords
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasOne(mr => mr.Patient)
                  .WithMany(p => p.MedicalRecords)
                  .HasForeignKey(mr => mr.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-set UpdatedAt on all modified BaseEntity records
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
