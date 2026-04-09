using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System.Text.Json;
using System.Text;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public BackupService(ApplicationDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<byte[]> GenerateDatabaseSnapshotAsync(string adminUserId)
    {
        var snapshot = new SystemSnapshotDto
        {
            GeneratedByAdminId = adminUserId,
            TotalPatients = await _context.Patients.CountAsync(),
            TotalDoctors = await _context.Doctors.CountAsync(),
            TotalRecords = await _context.MedicalRecords.CountAsync(),
            TotalAppointments = await _context.Appointments.CountAsync(),
            TotalAuditLogs = await _context.AuditLogs.CountAsync(),

            Patients = await _context.Patients
                .Select(p => new PatientSnapshot
                {
                    Id = p.Id,
                    FirstName = p.User.FirstName,
                    LastName = p.User.LastName,
                    Email = p.User.Email,
                    CreatedAt = p.CreatedAt
                }).ToListAsync(),

            Doctors = await _context.Doctors
                .Select(d => new DoctorSnapshot
                {
                    Id = d.Id,
                    FirstName = d.User.FirstName,
                    LastName = d.User.LastName,
                    Specialization = d.Specialization ?? "General",
                    Email = d.User.Email
                }).ToListAsync(),

            MedicalRecords = await _context.MedicalRecords
                .Select(r => new RecordSnapshot
                {
                    Id = r.Id,
                    PatientId = r.PatientId,
                    DoctorId = r.AssignedDoctorId,
                    Title = r.OriginalFileName,
                    Type = r.RecordType ?? "General",
                    CreatedAt = r.CreatedAt
                }).ToListAsync(),

            Appointments = await _context.Appointments
                .Select(a => new AppointmentSnapshot
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    DoctorId = a.DoctorId,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status.ToString()
                }).ToListAsync()
        };

        // Log the action securely
        Guid.TryParse(adminUserId, out Guid parsedAdminId);
        await _auditLogService.LogAsync(
            parsedAdminId,
            "System Backup Downloaded",
            $"Admin {adminUserId} downloaded a full system structural snapshot.",
            "N/A",
            "Secure Medical Record System Backup Agent",
            "System",
            null,
            SecureMedicalRecordSystem.Core.Enums.AuditSeverity.Info
        );

        // Serialize and format directly to a byte array
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(snapshot, options);
        return Encoding.UTF8.GetBytes(jsonString);
    }
}
