using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecureMedicalRecordSystem.Core.DTOs.Doctor;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IDoctorStatisticsService
{
    Task<DoctorDashboardStatsDTO> GetDashboardStatisticsAsync(Guid doctorId);
    Task<List<TodayAppointmentDTO>> GetTodayScheduleAsync(Guid doctorId);
    Task<WeekScheduleDTO> GetWeekScheduleAsync(Guid doctorId);
    Task<List<PatientVolumeTrendDTO>> GetPatientVolumeTrendAsync(Guid doctorId);
    Task<RecordStatusBreakdownDTO> GetRecordStatusBreakdownAsync(Guid doctorId);
    Task<List<RecentScanDTO>> GetRecentScansAsync(Guid doctorId, int limit);
    Task<List<TemplateUsageDTO>> GetTemplateUsageAsync(Guid doctorId);
    Task<List<RecordGrowthTrendDTO>> GetRecordGrowthTrendAsync(Guid doctorId);
}
