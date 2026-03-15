using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Roles = "Admin")]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICachingService _cache;
    private const string DeptCacheKey = "lookups:departments";

    public DepartmentsController(ApplicationDbContext context, ICachingService cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    [AllowAnonymous] // Patient upload needs to see these too
    public async Task<IActionResult> GetAll()
    {
        var departments = await _cache.GetOrSetAsync(DeptCacheKey, async () =>
        {
            return await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Description,
                    d.IsActive,
                    DoctorCount = d.Doctors.Count
                })
                .ToListAsync();
        }, TimeSpan.FromHours(1));

        return Ok(ApiResponse.SuccessResult(departments, "Departments retrieved successfully."));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var department = await _context.Departments
            .Include(d => d.Doctors)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null) return NotFound(ApiResponse.FailureResult("Department not found."));

        return Ok(ApiResponse.SuccessResult(new
        {
            department.Id,
            department.Name,
            department.Description,
            department.IsActive,
            Doctors = department.Doctors.Select(doc => new { doc.Id, Name = $"Dr. {doc.User.FirstName} {doc.User.LastName}" })
        }, "Department details retrieved."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Department department)
    {
        if (await _context.Departments.AnyAsync(d => d.Name == department.Name))
        {
            return BadRequest(ApiResponse.FailureResult("Department with this name already exists."));
        }

        department.Id = Guid.NewGuid();
        department.CreatedAt = DateTime.UtcNow;
        department.CreatedBy = User.Identity?.Name ?? "Admin";

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        await _cache.InvalidateAsync(DeptCacheKey);

        return CreatedAtAction(nameof(GetById), new { id = department.Id }, ApiResponse.SuccessResult(department, "Department created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Department updateDto)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound(ApiResponse.FailureResult("Department not found."));

        department.Name = updateDto.Name;
        department.Description = updateDto.Description;
        department.IsActive = updateDto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = User.Identity?.Name ?? "Admin";

        await _context.SaveChangesAsync();
        await _cache.InvalidateAsync(DeptCacheKey);
        return Ok(ApiResponse.SuccessResult(department, "Department updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var department = await _context.Departments.Include(d => d.Doctors).FirstOrDefaultAsync(d => d.Id == id);
        if (department == null) return NotFound(ApiResponse.FailureResult("Department not found."));

        if (department.Doctors.Any())
        {
            return BadRequest(ApiResponse.FailureResult("Cannot delete department with assigned doctors. Move doctors first."));
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        await _cache.InvalidateAsync(DeptCacheKey);

        return Ok(ApiResponse.SuccessResult((object?)null, "Department deleted successfully."));
    }
}
