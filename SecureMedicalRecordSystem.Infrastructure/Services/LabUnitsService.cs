using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs.LabUnits;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System.Text.Json;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class LabUnitsService : ILabUnitsService
{
    private readonly ApplicationDbContext _context;

    public LabUnitsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LabUnitDTO>> SearchLabUnitsAsync(string query)
    {
        var lowerQuery = query.ToLower();
        
        var results = await _context.CommonLabUnits
            .Where(u => u.MeasurementType.ToLower().Contains(lowerQuery) || 
                        u.Aliases.ToLower().Contains(lowerQuery) ||
                        u.Category.ToLower().Contains(lowerQuery))
            .Take(20)
            .ToListAsync();

        return results.Select(MapToDTO).ToList();
    }

    public async Task<LabUnitDTO?> GetLabUnitByIdAsync(Guid id)
    {
        var unit = await _context.CommonLabUnits.FindAsync(id);
        return unit == null ? null : MapToDTO(unit);
    }

    public async Task<(bool Success, string Message, LabUnitDTO? Data)> CreateCustomLabUnitAsync(CreateCustomLabUnitDTO request, Guid doctorId)
    {
        var existing = await _context.CommonLabUnits
            .FirstOrDefaultAsync(u => u.MeasurementType.ToLower() == request.MeasurementType.ToLower());
        
        if (existing != null)
        {
            return (false, "A measurement with this name already exists", MapToDTO(existing));
        }

        var newUnit = new CommonLabUnit
        {
            MeasurementType = request.MeasurementType,
            CommonUnits = JsonSerializer.Serialize(request.CommonUnits),
            DefaultUnit = request.DefaultUnit,
            NormalRangeLow = request.NormalRangeLow,
            NormalRangeHigh = request.NormalRangeHigh,
            NormalRangeUnit = request.NormalRangeUnit,
            Category = request.Category ?? "Custom",
            CreatedBy = doctorId.ToString()
        };

        await _context.CommonLabUnits.AddAsync(newUnit);
        await _context.SaveChangesAsync();

        return (true, "Custom measurement added successfully", MapToDTO(newUnit));
    }

    private LabUnitDTO MapToDTO(CommonLabUnit u)
    {
        return new LabUnitDTO
        {
            Id = u.Id,
            MeasurementType = u.MeasurementType,
            CommonUnits = TryDeserialize(u.CommonUnits),
            DefaultUnit = u.DefaultUnit,
            NormalRangeLow = u.NormalRangeLow,
            NormalRangeHigh = u.NormalRangeHigh,
            NormalRangeUnit = u.NormalRangeUnit,
            Aliases = TryDeserialize(u.Aliases),
            Category = u.Category
        };
    }

    private List<string> TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
