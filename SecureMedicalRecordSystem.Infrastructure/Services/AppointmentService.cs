using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SecureMedicalRecordSystem.Core.DTOs.Appointments;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDoctorAvailabilityService _availabilityService;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AppointmentService(
        ApplicationDbContext context,
        IEmailService emailService,
        IAuditLogService auditLogService,
        IDoctorAvailabilityService availabilityService,
        ILogger<AppointmentService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _availabilityService = availabilityService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<(bool Success, string Message, AppointmentDTO? Data)> CreateAppointmentAsync(
        CreateAppointmentDTO request, 
        Guid requestingUserId)
    {
        try
        {
            _logger.LogInformation("Creating appointment for patient user {UserId} with Dr {DoctorId} at {Date}", requestingUserId, request.DoctorId, request.AppointmentDate);

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == requestingUserId);

            if (patient == null)
            {
                _logger.LogWarning("Patient profile not found for user {UserId}", requestingUserId);
                return (false, "Patient profile not found.", null);
            }

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId);

            if (doctor == null)
            {
                _logger.LogWarning("Doctor with ID {DoctorId} not found.", request.DoctorId);
                return (false, "Doctor not found.", null);
            }

            // Ensure we treat the incoming date correctly (treat as UTC if unspecified)
            var appointmentDate = request.AppointmentDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.AppointmentDate, DateTimeKind.Utc) 
                : request.AppointmentDate.ToUniversalTime();

            // Validate date
            if (appointmentDate <= DateTime.Now)
            {
                _logger.LogWarning("Appointment date {Date} is not in the future (Now UTC: {Now})", appointmentDate, DateTime.Now);
                return (false, "Appointment date must be in the future.", null);
            }

            // Check conflicts using rules + appointments
            var duration = request.Duration;
            var isAvailable = await _availabilityService.IsDoctorAvailableAsync(
                request.DoctorId, 
                appointmentDate, 
                duration);

            if (!isAvailable)
            {
                _logger.LogWarning("Doctor {DoctorId} is not available at {Date}", request.DoctorId, appointmentDate);
                return (false, "This time slot is no longer available. Please select another time.", null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    DoctorId = request.DoctorId,
                    AppointmentDate = appointmentDate,
                    Duration = duration,
                    ReasonForVisit = request.ReasonForVisit,
                    Status = AppointmentStatus.Confirmed,
                    CreatedAt = DateTime.Now,
                    ScheduledAt = appointmentDate,
                    ConfirmedAt = DateTime.Now,
                    CreatedBy = requestingUserId,
                    IsActive = true
                };

                await _context.Appointments.AddAsync(appointment);

                // Set as primary doctor if not already set
                if (patient.PrimaryDoctorId == null)
                {
                    patient.PrimaryDoctorId = request.DoctorId;
                    _logger.LogInformation("Setting Dr {DoctorId} as primary for patient {PatientId} during appointment creation", request.DoctorId, patient.Id);
                }

                await _context.SaveChangesAsync();
                
                // Double check for any overlaps created at the exact same millisecond
                if (await CheckAppointmentConflictAsync(request.DoctorId, appointmentDate, duration, appointment.Id))
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Race condition: slot {Date} for Dr {DoctorId} was just taken", appointmentDate, request.DoctorId);
                    return (false, "This time slot was just booked by someone else. Please try another.", null);
                }

                await transaction.CommitAsync();

                await _auditLogService.LogAsync(
                    requestingUserId,
                    "Appointment scheduled (Instant)",
                    $"Confirmed appointment with Dr. {doctor.User.LastName} on {appointmentDate:g}",
                    "0.0.0.0", "Service", "Appointment", appointment.Id.ToString());

                // Set navigation properties for MapToDTO
                appointment.Patient = patient;
                appointment.Doctor = doctor;

                // Fire-and-forget: return instantly after DB commit, emails go out in background
                var patientEmail = patient.User.Email!;
                var doctorEmail = doctor.User.Email!;
                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    try 
                    {
                        await emailSvc.SendAppointmentConfirmationEmailAsync(patientEmail, appointment);
                        await emailSvc.SendDoctorNewAppointmentNotificationAsync(doctorEmail, appointment);
                    }
                    catch (Exception emailEx)
                    {
                        Log.Error(emailEx, "Background email failed for appointment {Id}", appointment.Id);
                    }
                });

                _logger.LogInformation("Appointment {Id} created successfully", appointment.Id);
                return (true, "Appointment confirmed successfully", MapToDTO(appointment));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed while creating appointment");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment for user {UserId}", requestingUserId);
            return (false, "An error occurred while scheduling the appointment.", null);
        }
    }

    public async Task<(bool Success, string Message, List<AppointmentDTO>? Data)> GetPatientAppointmentsAsync(
        Guid userId, 
        Guid requestingUserId, 
        bool includeHistory = false)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (patient == null) 
        {
            _logger.LogWarning("No patient profile found for user {UserId}", userId);
            return (true, "No patient profile found. Returning empty list.", new List<AppointmentDTO>());
        }

        var query = _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.LinkedRecords).ThenInclude(ar => ar.MedicalRecord)
            .Where(a => a.PatientId == patient.Id); // Fix: use patient.Id instead of userId

        if (!includeHistory)
        {
            var cutoffDate = DateTime.Now.AddDays(-7);
            query = query.Where(a => a.AppointmentDate >= cutoffDate || 
                                   a.Status == AppointmentStatus.Scheduled ||
                                   a.Status == AppointmentStatus.Confirmed);
        }

        var appointments = await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
        return (true, "Success", appointments.Select(MapToDTO).ToList());
    }

    public async Task<(bool Success, string Message, AppointmentDTO? Data)> GetAppointmentByIdAsync(
        Guid appointmentId, 
        Guid requestingUserId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.LinkedRecords).ThenInclude(ar => ar.MedicalRecord)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null) return (false, "Appointment not found.", null);

        // Security check: only involved parties or admins can view
        var user = await _context.Users.FindAsync(requestingUserId);
        var isAdmin = user?.Role == "Admin";
        
        if (appointment.Patient.UserId != requestingUserId && 
            appointment.Doctor.UserId != requestingUserId && 
            !isAdmin)
        {
            return (false, "You do not have permission to view this appointment.", null);
        }

        return (true, "Success", MapToDTO(appointment));
    }

    public async Task<(bool Success, string Message, List<AppointmentDTO>? Data)> GetDoctorAppointmentsAsync(
        Guid doctorId, 
        Guid requestingUserId, 
        DateTime? date = null,
        bool includeHistory = false)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null) return (false, "Doctor not found.", null);

        var query = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.LinkedRecords).ThenInclude(ar => ar.MedicalRecord)
            .Where(a => a.DoctorId == doctorId && a.IsActive);

        if (includeHistory)
        {
            // No date filtering, return all
        }
        else if (date.HasValue)
        {
            var startOfDay = date.Value.Date;
            var endOfDay = startOfDay.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= startOfDay && a.AppointmentDate < endOfDay);

            // For active schedule, exclude cancelled, completed, and noshow
            query = query.Where(a => a.Status != AppointmentStatus.Cancelled && 
                                   a.Status != AppointmentStatus.Completed &&
                                   a.Status != AppointmentStatus.NoShow);
        }
        else
        {
            var today = DateTime.UtcNow.Date;
            var futureDate = today.AddDays(90); // Increased to 90 days for better calendar visibility
            query = query.Where(a => a.AppointmentDate >= today && a.AppointmentDate <= futureDate);

            // For active schedule, exclude cancelled, completed, and noshow
            query = query.Where(a => a.Status != AppointmentStatus.Cancelled && 
                                   a.Status != AppointmentStatus.Completed &&
                                   a.Status != AppointmentStatus.NoShow);
        }

        var appointments = await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
        return (true, "Success", appointments.Select(MapToDTO).ToList());
    }

    public async Task<(bool Success, string Message, DoctorAppointmentStatsDTO? Data)> GetDoctorStatsAsync(
        Guid doctorId,
        Guid requestingUserId)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null) return (false, "Doctor not found.", null);

        var now = DateTime.Now;
        var today = now.Date;
        var endOfToday = today.AddDays(1);

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.IsActive)
            .ToListAsync();

        var stats = new DoctorAppointmentStatsDTO
        {
            TotalAppointments = appointments.Count,
            CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
            UpcomingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Confirmed && a.AppointmentDate > now),
            CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            PendingConfirmation = appointments.Count(a => a.Status == AppointmentStatus.Scheduled && a.AppointmentDate > now),
            TodayAppointments = appointments.Count(a => a.AppointmentDate >= today && a.AppointmentDate < endOfToday && !a.IsCancelled)
        };

        return (true, "Success", stats);
    }

    public async Task<(bool Success, string Message)> CancelAppointmentAsync(
        Guid appointmentId, 
        string cancellationReason, 
        Guid requestingUserId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null) return (false, "Appointment not found.");

        if (appointment.Status == AppointmentStatus.Cancelled || appointment.Status == AppointmentStatus.Completed)
            return (false, "Appointment already cancelled or completed.");

        // Business Rule: Patient can only cancel up to 24 hours before
        var isPatient = appointment.Patient.UserId == requestingUserId;
        if (isPatient && (appointment.AppointmentDate - DateTime.Now).TotalHours < 24)
        {
            return (false, "Appointments cannot be cancelled by patients within 24 hours of the scheduled time.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.IsCancelled = true;
        appointment.CancelledAt = DateTime.Now;
        appointment.CancellationReason = cancellationReason;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            requestingUserId,
            "Appointment cancelled",
            $"Appointment {appointmentId} cancelled by {(isPatient ? "Patient" : "Doctor")}. Reason: {cancellationReason}",
            "0.0.0.0", "Service", "Appointment", appointment.Id.ToString());

        // Fire-and-forget cancellation notifications
        var pEmail = appointment.Patient.User.Email!;
        var dEmail = appointment.Doctor.User.Email!;
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
            try
            {
                await emailSvc.SendAppointmentCancelledEmailAsync(pEmail, appointment, cancellationReason);
                await emailSvc.SendAppointmentCancelledEmailAsync(dEmail, appointment, cancellationReason);
            }
            catch (Exception ex) { Log.Error(ex, "Background cancel email failed for appointment {Id}", appointment.Id); }
        });

        return (true, "Appointment cancelled successfully.");
    }

    public async Task<(bool Success, string Message, AppointmentDTO? Data)> RescheduleAppointmentAsync(
        Guid appointmentId, 
        DateTime newDateTime, 
        Guid requestingUserId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null) return (false, "Appointment not found.", null);

        if (appointment.Status == AppointmentStatus.Completed)
            return (false, "Completed appointments cannot be rescheduled.", null);

        if (newDateTime <= DateTime.Now)
            return (false, "New date must be in the future.", null);

        if (await CheckAppointmentConflictAsync(appointment.DoctorId, newDateTime, appointment.Duration))
            return (false, "New time slot not available.", null);

        var oldDate = appointment.AppointmentDate;
        appointment.AppointmentDate = newDateTime;
        appointment.Status = AppointmentStatus.Scheduled; // Re-requires confirmation
        appointment.ConfirmedAt = null;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            requestingUserId,
            "Appointment rescheduled",
            $"Moved from {oldDate:g} to {newDateTime:g}",
            "0.0.0.0", "Service", "Appointment", appointment.Id.ToString());

        // Fire-and-forget reschedule notification
        var reschedEmail = appointment.Patient.User.Email!;
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
            try { await emailSvc.SendAppointmentRescheduledEmailAsync(reschedEmail, appointment); }
            catch (Exception ex) { Log.Error(ex, "Background reschedule email failed for appointment {Id}", appointment.Id); }
        });

        return (true, "Appointment rescheduled successfully.", MapToDTO(appointment));
    }


    public async Task<(bool Success, string Message)> CompleteAppointmentAsync(
        Guid appointmentId, 
        string consultationNotes, 
        Guid requestingUserId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null) return (false, "Appointment not found.");

        if (appointment.Status != AppointmentStatus.InProgress && appointment.Status != AppointmentStatus.Confirmed)
            return (false, "Appointment must be Confirmed or In Progress to be completed.");

        if (string.IsNullOrWhiteSpace(consultationNotes))
            return (false, "Consultation notes are required to complete the appointment.");

        appointment.Status = AppointmentStatus.Completed;
        appointment.IsCompleted = true;
        appointment.CompletedAt = DateTime.Now;
        appointment.ConsultationNotes = consultationNotes;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            requestingUserId,
            "Appointment completed",
            $"Consultation notes added. Records linked: {appointment.LinkedRecords?.Count ?? 0}",
            "0.0.0.0", "Service", "Appointment", appointment.Id.ToString());

        return (true, "Appointment marked as completed successfully.");
    }

    public async Task<(bool Success, string Message)> ConfirmAppointmentAsync(
        Guid appointmentId, 
        Guid requestingUserId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null) return (false, "Appointment not found.");

        if (appointment.Status != AppointmentStatus.Scheduled)
            return (false, "Only scheduled appointments can be confirmed.");

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.ConfirmedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Fire-and-forget confirmation notification
        var confirmEmail = appointment.Patient.User.Email!;
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
            try { await emailSvc.SendAppointmentConfirmedEmailAsync(confirmEmail, appointment); }
            catch (Exception ex) { Log.Error(ex, "Background confirm email failed for appointment {Id}", appointment.Id); }
        });

        return (true, "Appointment confirmed.");
    }

    public async Task<(bool Success, string Message)> LinkRecordToAppointmentAsync(
        Guid appointmentId, 
        Guid medicalRecordId, 
        string notes, 
        Guid requestingUserId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        var record = await _context.MedicalRecords.FindAsync(medicalRecordId);

        if (appointment == null || record == null) return (false, "Appointment or record not found.");

        if (record.PatientId != appointment.PatientId)
            return (false, "Record does not belong to this patient.");

        var existingLink = await _context.AppointmentRecords
            .AnyAsync(ar => ar.AppointmentId == appointmentId && ar.MedicalRecordId == medicalRecordId);

        if (existingLink)
            return (false, "Record already linked.");

        var link = new AppointmentRecord
        {
            AppointmentId = appointmentId,
            MedicalRecordId = medicalRecordId,
            LinkedAt = DateTime.Now,
            LinkedBy = requestingUserId,
            Notes = notes
        };

        await _context.AppointmentRecords.AddAsync(link);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            requestingUserId,
            "Medical record linked to appointment",
            $"Record {record.OriginalFileName} linked",
            "0.0.0.0", "Service", "Appointment", appointment.Id.ToString());

        return (true, "Record linked successfully.");
    }

    public async Task<List<TimeSlotDTO>> GetDoctorAvailableSlotsAsync(
        Guid doctorId, 
        DateTime date)
    {
        // Use the centralized availability service which handles rules, blocks, and UTC conversion correctly
        return await _availabilityService.GetAvailableSlotsWithRulesAsync(doctorId, date);
    }

    public async Task<bool> CheckAppointmentConflictAsync(
        Guid doctorId, 
        DateTime appointmentDateTime, 
        int duration = 30,
        Guid? excludeAppointmentId = null)
    {
        var appointmentStart = appointmentDateTime;
        var appointmentEnd = appointmentDateTime.AddMinutes(duration);
        // Resolve to a concrete Guid so EF Core LINQ can translate the != comparison
        var excludeId = excludeAppointmentId ?? Guid.Empty;

        return await _context.Appointments.AnyAsync(a => 
            a.DoctorId == doctorId &&
            !a.IsCancelled &&
            a.IsActive &&
            a.Id != excludeId &&
            (
                (appointmentStart >= a.AppointmentDate && appointmentStart < a.AppointmentDate.AddMinutes(a.Duration)) ||
                (appointmentEnd > a.AppointmentDate && appointmentEnd <= a.AppointmentDate.AddMinutes(a.Duration)) ||
                (appointmentStart <= a.AppointmentDate && appointmentEnd >= a.AppointmentDate.AddMinutes(a.Duration))
            )
        );
    }

    public async Task<(bool Success, string Message, SmartDoctorSuggestionDTO? Data)> GetSmartDoctorSuggestionsAsync(
        Guid requestingUserId)
    {
        var patient = await _context.Patients
            .Include(p => p.PrimaryDoctor).ThenInclude(d => d!.User)
            .Include(p => p.PrimaryDoctor).ThenInclude(d => d!.Department)
            .FirstOrDefaultAsync(p => p.UserId == requestingUserId);

        if (patient == null) return (false, "Patient profile not found.", null);

        var now = DateTime.Now;
        var suggestion = new SmartDoctorSuggestionDTO();

        // 1. Upcoming appointment context
        var upcoming = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Where(a => a.PatientId == patient.Id && a.ScheduledAt >= now && a.IsActive && !a.IsCancelled)
            .OrderBy(a => a.ScheduledAt)
            .FirstOrDefaultAsync();

        if (upcoming != null)
        {
            suggestion.UpcomingAppointmentDoctor = new DoctorSuggestionItem
            {
                Id = upcoming.DoctorId.ToString(),
                FullName = $"Dr. {upcoming.Doctor.User.FirstName} {upcoming.Doctor.User.LastName}",
                Department = upcoming.Doctor.Department?.Name ?? "General",
                SuggestionType = "Appointment",
                SuggestionLabel = $"Upcoming: {upcoming.ScheduledAt:MMM d}",
                ProfilePictureUrl = upcoming.Doctor.User.ProfilePictureUrl
            };
        }

        // 2. Primary Doctor
        if (patient.PrimaryDoctor != null)
        {
            suggestion.PrimaryDoctor = new DoctorSuggestionItem
            {
                Id = patient.PrimaryDoctor.Id.ToString(),
                FullName = $"Dr. {patient.PrimaryDoctor.User.FirstName} {patient.PrimaryDoctor.User.LastName}",
                Department = patient.PrimaryDoctor.Department?.Name ?? "General",
                SuggestionType = "Primary",
                SuggestionLabel = "Primary Care Provider",
                ProfilePictureUrl = patient.PrimaryDoctor.User.ProfilePictureUrl
            };
        }

        // 3. Recent Doctors (from last 5 completed appointments)
        var recentDoctors = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Where(a => a.PatientId == patient.Id && a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.CompletedAt)
            .Take(10)
            .ToListAsync();

        var seenIds = new HashSet<Guid>();
        foreach (var app in recentDoctors)
        {
            if (seenIds.Add(app.DoctorId))
            {
                var daysAgo = (int)(now - (app.CompletedAt ?? app.AppointmentDate)).TotalDays;
                var label = daysAgo == 0 ? "Today" : daysAgo == 1 ? "Yesterday" : $"{daysAgo} days ago";

                suggestion.RecentDoctors.Add(new DoctorSuggestionItem
                {
                    Id = app.DoctorId.ToString(),
                    FullName = $"Dr. {app.Doctor.User.FirstName} {app.Doctor.User.LastName}",
                    Department = app.Doctor.Department?.Name ?? "General",
                    SuggestionType = "Recent",
                    SuggestionLabel = $"Last visit {label}",
                    ProfilePictureUrl = app.Doctor.User.ProfilePictureUrl
                });

                if (seenIds.Count >= 3) break;
            }
        }

        // Priority chain
        suggestion.RecommendedDoctor = 
            suggestion.UpcomingAppointmentDoctor ?? 
            suggestion.PrimaryDoctor ?? 
            suggestion.RecentDoctors.FirstOrDefault();

        return (true, "Success", suggestion);
    }

    public async Task<(bool Success, string Message, List<DoctorSuggestionItem>? Data)> SuggestDoctorsByReasonAsync(
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (true, "No reason provided", new List<DoctorSuggestionItem>());

        var query = reason.ToLowerInvariant();
        var departmentKeywords = new Dictionary<string, string[]>
        {
            { "Cardiology", new[] { "heart", "chest pain", "blood pressure", "ecg", "cardiac", "palpitations" } },
            { "Neurology", new[] { "headache", "migraine", "seizure", "brain", "nerve", "dizzy", "numbness" } },
            { "Endocrinology", new[] { "diabetes", "sugar", "thyroid", "hormone", "insulin", "metabolism" } },
            { "Orthopedics", new[] { "bone", "fracture", "joint", "knee", "back pain", "spine", "ortho" } },
            { "Dermatology", new[] { "skin", "rash", "acne", "mole", "itching", "eczema", "dermatologist" } },
            { "Pediatrics", new[] { "child", "baby", "infant", "pediatric", "vaccination", "growth" } },
            { "Gastroenterology", new[] { "stomach", "digestion", "liver", "gut", "acid reflux", "bloating" } }
        };

        string? suggestedDepartment = null;
        foreach (var entry in departmentKeywords)
        {
            if (entry.Value.Any(k => query.Contains(k)))
            {
                suggestedDepartment = entry.Key;
                break;
            }
        }

        if (suggestedDepartment == null) return (true, "No matches found", new List<DoctorSuggestionItem>());

        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Department)
            .Where(d => d.Department.Name == suggestedDepartment)
            .Take(3)
            .ToListAsync();

        var result = doctors.Select(d => new DoctorSuggestionItem
        {
            Id = d.Id.ToString(),
            FullName = $"Dr. {d.User.FirstName} {d.User.LastName}",
            Department = d.Department?.Name ?? suggestedDepartment,
            SuggestionType = "Recommendation",
            SuggestionLabel = $"Recommended for {suggestedDepartment}",
            ProfilePictureUrl = d.User.ProfilePictureUrl
        }).ToList();

        return (true, $"Found {result.Count} suggestions for {suggestedDepartment}", result);
    }

    public async Task<int> CheckAndTransitionAppointmentStatusesAsync()
    {
        var now = DateTime.Now;
        var fifteenMinutesAgo = now.AddMinutes(-15);
        
        // 1. Transition Scheduled/Confirmed to NoShow if 15 mins past start time
        var noShows = await _context.Appointments
            .Where(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
                     && a.AppointmentDate < fifteenMinutesAgo
                     && a.IsActive && !a.IsCancelled)
            .ToListAsync();

        foreach (var appt in noShows)
        {
            appt.Status = AppointmentStatus.NoShow;
            _logger.LogInformation("Appointment {Id} marked as NoShow (15 mins past start).", appt.Id);
        }

        // 2. Transition Confirmed to InProgress if start time has arrived
        var startedAppointments = await _context.Appointments
            .Where(a => a.Status == AppointmentStatus.Confirmed
                     && a.AppointmentDate <= now
                     && a.IsActive && !a.IsCancelled)
            .ToListAsync();

        foreach (var appt in startedAppointments)
        {
            appt.Status = AppointmentStatus.InProgress;
            _logger.LogInformation("Appointment {Id} transitioned to InProgress.", appt.Id);
        }

        if (noShows.Any() || startedAppointments.Any())
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(null, "Bulk appointment status transition", $"Processed {noShows.Count} NoShows and {startedAppointments.Count} InProgress.", "0.0.0.0", "System");
        }

        return noShows.Count + startedAppointments.Count;
    }

    private AppointmentDTO MapToDTO(Appointment appointment)
    {
        var now = DateTime.Now;
        var displayStatus = appointment.Status.ToString();

        // Dynamic status transition: Scheduled/Confirmed/InProgress -> Overdue if time exceeded
        if (!appointment.IsCompleted && !appointment.IsCancelled && 
            (appointment.Status == AppointmentStatus.Scheduled || 
             appointment.Status == AppointmentStatus.Confirmed || 
             appointment.Status == AppointmentStatus.InProgress) &&
            now > appointment.AppointmentDate.AddMinutes(appointment.Duration))
        {
            displayStatus = "Overdue";
        }

        return new AppointmentDTO
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient?.User != null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Unknown",
            PatientAge = appointment.Patient != null ? (DateTime.Now.Year - appointment.Patient.DateOfBirth.Year) : 0,
            PatientGender = appointment.Patient?.Gender ?? "Unknown",
            DoctorId = appointment.DoctorId,
            DoctorName = appointment.Doctor?.User != null ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "Unknown",
            DoctorDepartment = appointment.Doctor?.Department?.Name ?? "General",
            DoctorProfilePictureUrl = appointment.Doctor?.User?.ProfilePictureUrl,
            AppointmentDate = appointment.AppointmentDate,
            Status = displayStatus,
            ReasonForVisit = appointment.ReasonForVisit,
            ConsultationNotes = appointment.ConsultationNotes,
            Duration = appointment.Duration,
            LinkedRecordsCount = appointment.LinkedRecords?.Count ?? 0,
            LinkedRecords = appointment.LinkedRecords?.Select(ar => new LinkedRecordSummaryDTO
            {
                RecordId = ar.MedicalRecordId,
                RecordFileName = ar.MedicalRecord?.OriginalFileName ?? "Unknown",
                RecordType = ar.MedicalRecord?.MimeType ?? "Unknown",
                LinkedAt = ar.LinkedAt,
                Notes = ar.Notes
            }).ToList() ?? new(),
            CanCancel = !appointment.IsCancelled && !appointment.IsCompleted && (appointment.AppointmentDate - DateTime.Now).TotalHours > 24,
            CanReschedule = !appointment.IsCancelled && !appointment.IsCompleted && (appointment.AppointmentDate - DateTime.Now).TotalHours > 24,
            CreatedAt = appointment.CreatedAt,
            CompletedAt = appointment.CompletedAt
        };
    }

    public async Task<FollowUpAppointmentResult> ScheduleFollowUpAppointmentAsync(
        Guid? originalAppointmentId,
        DateTime preferredDate,
        Guid doctorId,
        Guid patientId,
        int duration)
    {
        try
        {
            // 1. Standardize the incoming date (same logic as CreateAppointmentAsync)
            var appointmentDate = preferredDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(preferredDate, DateTimeKind.Utc) 
                : preferredDate.ToUniversalTime();

            // 2. Must be in the future
            if (appointmentDate <= DateTime.UtcNow)
            {
                return new FollowUpAppointmentResult
                {
                    WasScheduled = false,
                    Message = "The proposed follow-up date must be in the future."
                };
            }

            // 3. Check for scheduling conflicts
            var hasConflict = await CheckAppointmentConflictAsync(doctorId, appointmentDate, duration);
            if (hasConflict)
            {
                // Format for message: h:mm tt (e.g. 5:15 PM)
                return new FollowUpAppointmentResult
                {
                    WasScheduled = false,
                    Message = $"The requested time slot ({appointmentDate.ToLocalTime():dddd, MMMM d} at {appointmentDate.ToLocalTime():h:mm tt}) is already booked."
                };
            }

            // 4. Validate doctor and patient exist
            var doctor = await _context.Doctors.Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doctorId);
            var patient = await _context.Patients.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (doctor == null || patient == null)
            {
                return new FollowUpAppointmentResult { WasScheduled = false, Message = "Doctor or patient not found." };
            }

            // 5. Create the follow-up appointment
            var followUpAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = appointmentDate,
                Duration = duration,
                ReasonForVisit = "Follow-up appointment",
                Status = AppointmentStatus.Scheduled,
                ParentAppointmentId = originalAppointmentId,
                CreatedAt = DateTime.Now,
                ScheduledAt = appointmentDate,
                IsActive = true,
                IsCancelled = false,
                IsCompleted = false,
                CreatedBy = doctorId
            };
;

            _context.Appointments.Add(followUpAppointment);
            await _context.SaveChangesAsync();

            // 7. Send follow-up confirmation email (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var patientEmail = patient.User?.Email ?? string.Empty;
                    var patientName = $"{patient.User?.FirstName} {patient.User?.LastName}";
                    var doctorName = $"{doctor.User?.FirstName} {doctor.User?.LastName}";
                    await _emailService.SendFollowUpConfirmationAsync(
                        patientEmail, patientName, doctorName, appointmentDate, duration, followUpAppointment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Follow-up confirmation email failed for appointment {Id}", followUpAppointment.Id);
                }
            });

            _logger.LogInformation("Follow-up appointment {Id} scheduled for {Date}", followUpAppointment.Id, appointmentDate);

            return new FollowUpAppointmentResult
            {
                WasScheduled = true,
                NewAppointmentId = followUpAppointment.Id,
                ScheduledFor = appointmentDate,
                Message = $"Follow-up appointment scheduled for {appointmentDate.ToLocalTime():dddd, MMMM d} at {appointmentDate.ToLocalTime():h:mm tt}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling follow-up appointment");
            return new FollowUpAppointmentResult
            {
                WasScheduled = false,
                Message = "An unexpected error occurred while scheduling the follow-up."
            };
        }
    }
}
