using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalFile> MedicalFiles => Set<MedicalFile>();
    public DbSet<RecordCertification> RecordCertifications => Set<RecordCertification>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentRecord> AppointmentRecords => Set<AppointmentRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<QRToken> QRTokens => Set<QRToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AccessSession> AccessSessions => Set<AccessSession>();
    public DbSet<TrustedDevice> TrustedDevices => Set<TrustedDevice>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<PatientHealthRecord> PatientHealthRecords => Set<PatientHealthRecord>();
    public DbSet<HealthAttribute> HealthAttributes => Set<HealthAttribute>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateUsageHistory> TemplateUsageHistory => Set<TemplateUsageHistory>();
    public DbSet<TemplateVersionHistory> TemplateVersionHistory => Set<TemplateVersionHistory>();
    public DbSet<DesktopSession> DesktopSessions => Set<DesktopSession>();
    public DbSet<MobileScannerPairing> MobileScannerPairings => Set<MobileScannerPairing>();
    public DbSet<ScanHistory> ScanHistories => Set<ScanHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // MUST BE FIRST for Identity

        // Rename Identity Tables
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        // Global query filters for soft delete (if applicable from base entity)
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Doctor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalFile>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RecordCertification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QRToken>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DoctorAvailability>().HasQueryFilter(e => !e.IsDeleted);
        
        // Added filters for dependent entities to match principals
        modelBuilder.Entity<AccessSession>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AppointmentRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DesktopSession>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MobileScannerPairing>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PatientHealthRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ScanHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TemplateUsageHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<HealthAttribute>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Template>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TemplateVersionHistory>().HasQueryFilter(e => !e.IsDeleted);

        // AuditLog specific configurations
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
        });

        // --------------------------------------------------------
        // RELATIONSHIPS & CONSTRAINTS (Phase 1 Identity)
        // --------------------------------------------------------

        // ApplicationUser -> Patient (One-to-One)
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithOne(u => u.PatientProfile)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ApplicationUser -> Doctor (One-to-One)
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.User)
            .WithOne(u => u.DoctorProfile)
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ApplicationUser -> AuditLogs (One-to-Many)
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull); // Keep audit logs even if user is deleted

        // Patient -> MedicalRecord (One-to-Many)
        modelBuilder.Entity<MedicalRecord>()
            .HasOne(mr => mr.Patient)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(mr => mr.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Patient -> Appointment (One-to-Many)
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.AppointmentDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate }); // Conflict check
            
            // Foreign keys
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.Appointments)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.Appointments)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Required fields
            entity.Property(e => e.AppointmentDate).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Duration).HasDefaultValue(30);
        });
            
        // Patient -> QRToken (One-to-Many)
        modelBuilder.Entity<QRToken>()
            .HasOne(q => q.Patient)
            .WithMany(p => p.QRTokens)
            .HasForeignKey(q => q.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Doctor -> RecordCertification (One-to-Many)
        modelBuilder.Entity<RecordCertification>()
            .HasOne(rc => rc.Doctor)
            .WithMany(d => d.Certifications)
            .HasForeignKey(rc => rc.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // AppointmentRecord (Junction Table)
        modelBuilder.Entity<AppointmentRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Composite unique index
            entity.HasIndex(e => new { e.AppointmentId, e.MedicalRecordId }).IsUnique();
            
            // Foreign keys
            entity.HasOne(e => e.Appointment)
                  .WithMany(a => a.LinkedRecords)
                  .HasForeignKey(e => e.AppointmentId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.MedicalRecord)
                  .WithMany()
                  .HasForeignKey(e => e.MedicalRecordId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DoctorAvailability configurations
        modelBuilder.Entity<DoctorAvailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes for fast lookups
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.DayOfWeek);
            entity.HasIndex(e => e.SpecificDate);
            entity.HasIndex(e => new { e.DoctorId, e.IsAvailable, e.IsActive }); // Common lookup
            
            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.Availabilities)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.RecurrenceType).IsRequired();
        });

        // MedicalRecord configurations
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.Property(m => m.S3ObjectKey).HasMaxLength(500).IsRequired();
            entity.Property(m => m.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(m => m.FileHash).HasMaxLength(100).IsRequired();
            entity.Property(m => m.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(m => m.EncryptionAlgorithm).HasMaxLength(50).HasDefaultValue("AES-256-CBC");
            entity.Property(m => m.IsEncrypted).HasDefaultValue(true);
            entity.Property(m => m.Version).HasDefaultValue(1);
            entity.Property(m => m.IsLatestVersion).HasDefaultValue(true);
            entity.Property(m => m.IsDeleted).HasDefaultValue(false);
        });

        // MedicalRecord -> RecordCertification (One-to-Many)
        modelBuilder.Entity<RecordCertification>()
            .HasOne(rc => rc.MedicalRecord)
            .WithMany(mr => mr.Certifications)
            .HasForeignKey(rc => rc.RecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // Department -> Doctor (One-to-Many)
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Department)
            .WithMany(dept => dept.Doctors)
            .HasForeignKey(d => d.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // AccessSession configurations
        modelBuilder.Entity<AccessSession>(entity =>
        {
            entity.HasIndex(s => s.SessionToken).IsUnique();
            
            // To avoid multiple cascade paths in SQL Server:
            // AccessSession -> Patient (Restrict)
            // AccessSession -> QRToken -> Patient (Cascade)
            entity.HasOne(s => s.Patient)
                .WithMany()
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(s => s.QRToken)
                .WithMany(q => q.AccessSessions)
                .HasForeignKey(s => s.QRTokenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --------------------------------------------------------
        // TRUSTED DEVICES
        // --------------------------------------------------------
        modelBuilder.Entity<TrustedDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceToken).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.TrustedDevices)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.DeviceToken).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
        });

        // --------------------------------------------------------
        // INDEXES FOR PERFORMANCE
        // --------------------------------------------------------

        // Existing unique indexes
        modelBuilder.Entity<Doctor>().HasIndex(d => d.NMCLicense).IsUnique();
        modelBuilder.Entity<QRToken>().HasIndex(q => q.Token).IsUnique();

        // Existing single-column indexes
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.Action);
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.Severity);
        modelBuilder.Entity<QRToken>().HasIndex(q => q.ExpiresAt);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.ScheduledAt);
        modelBuilder.Entity<Appointment>().HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.Status });
        modelBuilder.Entity<MedicalRecord>().HasIndex(m => new { m.AssignedDoctorId, m.CreatedAt, m.State });
        modelBuilder.Entity<ScanHistory>().HasIndex(s => new { s.DoctorId, s.ScannedAt });

        // --- NEW PERFORMANCE INDEXES ---

        // Doctor & Patient UserId lookups (FindByUserId is called on almost every request)
        modelBuilder.Entity<Doctor>().HasIndex(d => d.UserId)
            .HasDatabaseName("IX_Doctors_UserId");
        modelBuilder.Entity<Patient>().HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Patients_UserId");

        // MedicalRecord - PatientId (GetPatientRecordsAsync filters by this on every call)
        modelBuilder.Entity<MedicalRecord>().HasIndex(m => m.PatientId)
            .HasDatabaseName("IX_MedicalRecords_PatientId");

        // MedicalRecord - AssignedDoctorId (doctor pending/certified queries filter by this)
        modelBuilder.Entity<MedicalRecord>().HasIndex(m => m.AssignedDoctorId)
            .HasDatabaseName("IX_MedicalRecords_AssignedDoctorId");

        // MedicalRecord - State (filtering by Draft/Pending/Certified)
        modelBuilder.Entity<MedicalRecord>().HasIndex(m => m.State)
            .HasDatabaseName("IX_MedicalRecords_State");

        // MedicalRecord composite - covers GetCertified/GetPendingForDoctorAsync exactly
        modelBuilder.Entity<MedicalRecord>()
            .HasIndex(m => new { m.AssignedDoctorId, m.State, m.IsDeleted })
            .HasDatabaseName("IX_MedicalRecords_AssignedDoctor_State_Deleted");

        // MedicalRecord - PatientId + State composite (GetPatientRecordsAsync)
        modelBuilder.Entity<MedicalRecord>()
            .HasIndex(m => new { m.PatientId, m.IsDeleted })
            .HasDatabaseName("IX_MedicalRecords_PatientId_Deleted");

        // RecordCertification - DoctorId + IsValid (COUNT query in GetDoctorDetailsAsync)
        modelBuilder.Entity<RecordCertification>()
            .HasIndex(c => new { c.DoctorId, c.IsValid })
            .HasDatabaseName("IX_RecordCertifications_DoctorId_IsValid");

        // RecordCertification - RecordId (used in EXISTS and joins for every record lookup)
        modelBuilder.Entity<RecordCertification>()
            .HasIndex(c => c.RecordId)
            .HasDatabaseName("IX_RecordCertifications_RecordId");

        // --------------------------------------------------------
        // STRUCTURED DATA ENTRY SYSTEM (EAV PATTERN)
        // --------------------------------------------------------

        // PatientHealthRecord
        modelBuilder.Entity<PatientHealthRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.RecordDate);
            entity.HasIndex(e => e.TemplateId);
            
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.StructuredRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.StructuredRecords)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.SetNull);
            
            entity.Property(e => e.RecordDate).IsRequired();
            entity.Property(e => e.IsStructured).HasDefaultValue(true);
            
            // Precision for decimals
            entity.Property(e => e.Temperature).HasPrecision(18, 2);
            entity.Property(e => e.Weight).HasPrecision(18, 2);
            entity.Property(e => e.Height).HasPrecision(18, 2);
            entity.Property(e => e.BMI).HasPrecision(18, 2);
            entity.Property(e => e.SpO2).HasPrecision(18, 2);
        });

        // HealthAttribute
        modelBuilder.Entity<HealthAttribute>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.RecordId);
            entity.HasIndex(e => e.FieldName);
            entity.HasIndex(e => e.SectionName);
            entity.HasIndex(e => new { e.RecordId, e.DisplayOrder });
            
            entity.HasOne(e => e.HealthRecord)
                  .WithMany(r => r.CustomAttributes)
                  .HasForeignKey(e => e.RecordId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.FieldName).IsRequired();
            entity.Property(e => e.FieldLabel).IsRequired();
            entity.Property(e => e.FieldValue).IsRequired();
            
            // Precision for decimals
            entity.Property(e => e.NormalRangeMin).HasPrecision(18, 2);
            entity.Property(e => e.NormalRangeMax).HasPrecision(18, 2);
        });

        // Template
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => new { e.CreatedBy, e.TemplateName }).IsUnique();
            
            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.ParentTemplate)
                  .WithMany()
                  .HasForeignKey(e => e.BasedOnTemplateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SourceRecord)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedFromRecordId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(e => e.TemplateName).IsRequired();
            entity.Property(e => e.TemplateSchema).IsRequired();
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
        });

        // TemplateUsageHistory
        modelBuilder.Entity<TemplateUsageHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.UsedAt);
            
            entity.HasOne(e => e.Template)
                  .WithMany(t => t.UsageHistory)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Record)
                  .WithMany()
                  .HasForeignKey(e => e.RecordId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TemplateVersionHistory
        modelBuilder.Entity<TemplateVersionHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.TemplateId, e.Version });
            entity.HasIndex(e => e.ChangedAt);
            
            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --------------------------------------------------------
        // MOBILE SCANNER & DESKTOP PAIRING
        // --------------------------------------------------------

        // DesktopSession
        modelBuilder.Entity<DesktopSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => new { e.SessionId, e.IsActive });
            
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Doctor)
                  .WithMany()
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MobileScannerPairing
        modelBuilder.Entity<MobileScannerPairing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DesktopSessionId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.MobileDeviceId);
            
            entity.Property(e => e.MobileDeviceId).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.DesktopSession)
                  .WithMany(d => d.MobileScannerPairings)
                  .HasForeignKey(e => e.DesktopSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Doctor)
                  .WithMany()
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ScanHistory
        modelBuilder.Entity<ScanHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.DesktopSessionId);
            
            entity.HasOne(e => e.Patient)
                  .WithMany()
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Doctor)
                  .WithMany()
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.NoAction);
                  
            entity.HasOne(e => e.DesktopSession)
                  .WithMany(d => d.ScanHistories)
                  .HasForeignKey(e => e.DesktopSessionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // --------------------------------------------------------
        // DATA SEEDING
        // --------------------------------------------------------
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Cardiology", Description = "Heart and blood vessel diseases", CreatedAt = seedDate },
            new Department { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Neurology", Description = "Disorders of the nervous system", CreatedAt = seedDate },
            new Department { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Orthopedics", Description = "Bones, joints, ligaments, tendons, and muscles", CreatedAt = seedDate },
            new Department { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Pediatrics", Description = "Medical care of infants, children, and adolescents", CreatedAt = seedDate },
            new Department { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Oncology", Description = "Prevention, diagnosis, and treatment of cancer", CreatedAt = seedDate }
        );
    }
}
