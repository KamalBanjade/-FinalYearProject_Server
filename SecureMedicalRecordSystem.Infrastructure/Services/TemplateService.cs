using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TemplateService> _logger;
    private readonly IAuditLogService _auditLogService;

    public TemplateService(ApplicationDbContext context, ILogger<TemplateService> logger, IAuditLogService auditLogService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<(bool Success, string Message, TemplateDTO? Data)> CreateTemplateAsync(CreateTemplateDTO request, Guid creatorId)
    {
        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == creatorId);
            if (doctor == null) return (false, "Not authorized", null);

            // Check for duplicate name for this doctor
            var existing = await _context.Templates
                .FirstOrDefaultAsync(t => t.CreatorId == creatorId && t.TemplateName == request.TemplateName && t.IsActive);
            if (existing != null)
                return (false, "You already have a template with this name", null);

            var schemaJson = request.Schema != null
                ? JsonSerializer.Serialize(request.Schema)
                : JsonSerializer.Serialize(new TemplateSchemaDTO());

            var template = new Template
            {
                Id = Guid.NewGuid(),
                TemplateName = request.TemplateName,
                Description = request.Description,
                CreatedBy = creatorId.ToString(), // Auditing (string)
                CreatorId = creatorId, // Functional ID (Guid)
                Visibility = request.Visibility,
                DepartmentId = doctor.DepartmentId,
                TemplateSchema = schemaJson,
                Version = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Templates.AddAsync(template);

            var versionHistory = new TemplateVersionHistory
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Version = 1,
                ChangeType = ChangeType.Created,
                ChangeDescription = "Template created manually",
                ModifiedBy = creatorId,
                ModifierId = creatorId,
                NewSchema = schemaJson,
                ChangedAt = DateTime.UtcNow
            };
            await _context.TemplateVersionHistory.AddAsync(versionHistory);

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(creatorId, "Template created", $"Created template {request.TemplateName}", "0.0.0.0", "Service");

            return (true, "Template created successfully", MapToDTO(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template");
            return (false, "Error creating template", null);
        }
    }

    public async Task<(bool Success, string Message, TemplateDTO? Data)> CreateTemplateFromRecordAsync(Guid recordId, string templateName, string description, VisibilityLevel visibility, Guid creatorId)
    {
        try
        {
            var record = await _context.PatientHealthRecords
                .Include(r => r.CustomAttributes)
                .FirstOrDefaultAsync(r => r.Id == recordId);
                
            if (record == null)
                return (false, "Record not found", null);

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == creatorId);
            if (doctor == null) return (false, "Not authorized", null);

            var existingTemplateForDoctor = await _context.Templates
                .FirstOrDefaultAsync(t => t.CreatorId == doctor.UserId && t.TemplateName == templateName);
                
            if (existingTemplateForDoctor != null)
                return (false, "You already have a template with this name", null);

            // Dynamically build schema from record attributes
            var schema = new TemplateSchemaDTO();
            var groupedAttributes = record.CustomAttributes.GroupBy(a => string.IsNullOrEmpty(a.SectionName) ? "General" : a.SectionName);
            
            int sectionOrder = 1;
            foreach (var group in groupedAttributes)
            {
                var sectionDTO = new TemplateSectionDTO
                {
                    SectionName = group.Key,
                    DisplayOrder = sectionOrder++
                };
                
                int fieldOrder = 1;
                foreach (var attr in group.OrderBy(a => a.DisplayOrder))
                {
                    sectionDTO.Fields.Add(new TemplateFieldDTO
                    {
                        FieldName = attr.FieldName,
                        FieldLabel = attr.FieldLabel,
                        FieldType = attr.FieldType,
                        Unit = attr.FieldUnit,
                        NormalRangeMin = attr.NormalRangeMin,
                        NormalRangeMax = attr.NormalRangeMax,
                        IsRequired = attr.IsRequired,
                        DisplayOrder = fieldOrder++
                    });
                }
                schema.Sections.Add(sectionDTO);
            }

            var schemaJson = JsonSerializer.Serialize(schema);

            var template = new Template
            {
                Id = Guid.NewGuid(),
                TemplateName = templateName,
                Description = description,
                CreatedBy = doctor.UserId.ToString(),
                CreatorId = doctor.UserId,
                CreatedFromRecordId = recordId,
                Visibility = visibility,
                DepartmentId = doctor.DepartmentId,
                TemplateSchema = schemaJson,
                Version = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Templates.AddAsync(template);
            
            var versionHistory = new TemplateVersionHistory
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Version = 1,
                ChangeType = ChangeType.Created,
                ChangeDescription = "Template created from record",
                ModifiedBy = doctor.UserId,
                ModifierId = doctor.UserId,
                NewSchema = schemaJson,
                ChangedAt = DateTime.UtcNow
            };
            await _context.TemplateVersionHistory.AddAsync(versionHistory);
            
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(doctor.UserId, "Template created", $"Organically created template {templateName}", "0.0.0.0", "Service");

            return (true, "Template created successfully", MapToDTO(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template from record {RecordId}", recordId);
            return (false, "Error creating template", null);
        }
    }

    public async Task<(bool Success, string Message, TemplateDTO? Data)> UpdateTemplateAsync(Guid templateId, UpdateTemplateDTO request, Guid modifierId)
    {
        try
        {
            var template = await _context.Templates.FindAsync(templateId);
            if (template == null) return (false, "Template not found", null);

            if (template.CreatorId != modifierId) return (false, "Only the creator can modify this template", null);

            var oldSchema = template.TemplateSchema;
            bool schemaChanged = false;

            if (request.TemplateName != null) template.TemplateName = request.TemplateName;
            if (request.Description != null) template.Description = request.Description;
            if (request.Visibility.HasValue) template.Visibility = request.Visibility.Value;
            if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;

            if (request.Schema != null)
            {
                // Self-healing: Preserve any retired fields from the old schema that the frontend might have accidentally omitted.
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                    var oldSchemaObj = JsonSerializer.Deserialize<TemplateSchemaDTO>(
                        oldSchema,
                        options
                    );
                    if (oldSchemaObj?.Sections != null && request.Schema.Sections != null)
                    {
                        foreach (var oldSection in oldSchemaObj.Sections)
                        {
                            var oldRetiredFields = oldSection.Fields?.Where(f => f.IsRetired).ToList() ?? new List<TemplateFieldDTO>();
                            if (oldRetiredFields.Any())
                            {
                                var newSection = request.Schema.Sections.FirstOrDefault(s => s.SectionName == oldSection.SectionName);
                                if (newSection == null)
                                {
                                    newSection = new TemplateSectionDTO 
                                    { 
                                        SectionName = oldSection.SectionName, 
                                        DisplayOrder = oldSection.DisplayOrder,
                                        Fields = new List<TemplateFieldDTO>()
                                    };
                                    request.Schema.Sections.Add(newSection);
                                }
                                
                                newSection.Fields ??= new List<TemplateFieldDTO>();
                                foreach (var retiredField in oldRetiredFields)
                                {
                                    if (!newSection.Fields.Any(f => f.FieldName == retiredField.FieldName))
                                    {
                                        newSection.Fields.Add(retiredField);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to self-heal template schema during update.");
                }

                var newSchemaJson = JsonSerializer.Serialize(
                    request.Schema,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        DefaultIgnoreCondition = 
                            System.Text.Json.Serialization.JsonIgnoreCondition.Never
                    }
                );
                if (newSchemaJson != oldSchema)
                {
                    template.TemplateSchema = newSchemaJson;
                    template.Version++;
                    schemaChanged = true;
                    
                    var versionHistory = new TemplateVersionHistory
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = template.Id,
                        Version = template.Version,
                        ChangeType = ChangeType.FieldModified,
                        ChangeDescription = "Template schema updated",
                        ModifiedBy = modifierId,
                        ModifierId = modifierId,
                        PreviousSchema = oldSchema,
                        NewSchema = newSchemaJson,
                        ChangedAt = DateTime.UtcNow
                    };
                    await _context.TemplateVersionHistory.AddAsync(versionHistory);
                }
            }

            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            var msg = schemaChanged ? "Template schema updated" : "Template details updated";
            await _auditLogService.LogAsync(modifierId, "Template updated", msg, "0.0.0.0", "Service");

            return (true, "Template updated successfully", MapToDTO(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
            return (false, "Error updating template", null);
        }
    }

    public async Task<(bool Success, string Message, List<TemplateDTO>? Data)> GetDoctorTemplatesAsync(Guid doctorId, bool includeShared = true)
    {
        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorId);
            if (doctor == null) return (false, "Doctor not found", null);

            var query = _context.Templates.AsQueryable();

            if (includeShared)
            {
                query = query.Where(t => 
                    t.CreatorId == doctorId || 
                    (t.Visibility == VisibilityLevel.Department && t.DepartmentId == doctor.DepartmentId) || 
                    t.Visibility == VisibilityLevel.Hospital);
            }
            else
            {
                query = query.Where(t => t.CreatorId == doctorId);
            }

            var templates = await query.Where(t => t.IsActive).ToListAsync();
            return (true, "Templates retrieved", templates.Select(MapToDTO).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for doctor {DoctorId}", doctorId);
            return (false, "Error retrieving templates", null);
        }
    }

    public async Task<(bool Success, string Message, List<TemplateDTO>? Data)> SuggestTemplatesAsync(string chiefComplaint, Guid doctorId)
    {
        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorId);
            if (doctor == null) return (false, "Doctor not found", null);

            var query = _context.Templates.AsQueryable()
                .Where(t => t.IsActive && (
                    t.CreatorId == doctorId || 
                    (t.Visibility == VisibilityLevel.Department && t.DepartmentId == doctor.DepartmentId) || 
                    t.Visibility == VisibilityLevel.Hospital));

            var allTemplates = await query.ToListAsync();
            
            if (string.IsNullOrWhiteSpace(chiefComplaint))
            {
                // Return top used templates if no complaint
                return (true, "Recent templates suggested", 
                    allTemplates.OrderByDescending(t => t.UsageCount).Take(5).Select(MapToDTO).ToList());
            }

            var complaintWords = chiefComplaint.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var matched = allTemplates
                .Select(t => new { 
                    Template = t, 
                    Score = CalculateMatchScore(t, complaintWords) 
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Template.UsageCount)
                .Take(5)
                .Select(x => x.Template)
                .ToList();

            // Always fill up to 5 suggestions if possible
            var result = new List<Template>(matched);
            if (result.Count < 5)
            {
                var extra = allTemplates
                    .Where(t => result.All(r => r.Id != t.Id))
                    .OrderByDescending(t => t.UsageCount)
                    .Take(5 - result.Count)
                    .ToList();
                result.AddRange(extra);
            }

            return (true, "Smart suggestions generated", result.Select(MapToDTO).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting templates");
            return (false, "Error suggesting templates", null);
        }
    }

    private int CalculateMatchScore(Template t, string[] queryWords)
    {
        int score = 0;
        var nameLower = t.TemplateName.ToLower();
        var descLower = (t.Description ?? "").ToLower();

        foreach (var word in queryWords)
        {
            if (nameLower.Contains(word)) score += 10;
            if (descLower.Contains(word)) score += 2;
        }

        // Exact name match bonus
        if (queryWords.Any(w => nameLower == w)) score += 20;

        return score;
    }

    public async Task<(bool Success, string Message)> DeleteTemplateAsync(Guid templateId, Guid doctorId)
    {
        var template = await _context.Templates.FindAsync(templateId);
        if (template == null) return (false, "Template not found");
        if (template.CreatorId != doctorId) return (false, "Only creator can delete");

        template.IsActive = false;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(doctorId, "Template deleted", $"Deactivated template {template.TemplateName}", "0.0.0.0", "Service");
        
        return (true, "Template deleted");
    }

    public async Task<(bool Success, string Message, TemplateDTO? Data)> ForkTemplateAsync(Guid sourceTemplateId, string newTemplateName, Guid doctorId)
    {
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorId);
        if (doctor == null) return (false, "Not authorized", null);

        var sourceTemplate = await _context.Templates.FindAsync(sourceTemplateId);
        if (sourceTemplate == null) return (false, "Source not found", null);

        var existing = await _context.Templates.FirstOrDefaultAsync(t => t.CreatorId == doctorId && t.TemplateName == newTemplateName);
        if (existing != null) return (false, "Name already exists", null);

        var newTemplate = new Template
        {
            Id = Guid.NewGuid(),
            TemplateName = newTemplateName,
            Description = $"Forked from {sourceTemplate.TemplateName}",
            CreatedBy = doctorId.ToString(),
            CreatorId = doctorId,
            BasedOnTemplateId = sourceTemplateId,
            Visibility = VisibilityLevel.Private,
            DepartmentId = doctor.DepartmentId,
            TemplateSchema = sourceTemplate.TemplateSchema, // Exact copy initially
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Templates.AddAsync(newTemplate);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(doctorId, "Template forked", $"Forked template as {newTemplateName}", "0.0.0.0", "Service");

        return (true, "Template forked successfully", MapToDTO(newTemplate));
    }

    public async Task<(bool Success, string Message, TemplateDTO? Data)> GetTemplateAsync(Guid templateId, Guid doctorId)
    {
        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorId);
            var template = await _context.Templates.FindAsync(templateId);
            
            if (template == null || !template.IsActive) return (false, "Template not found", null);

            // Access check: own, same department, or hospital-wide
            bool hasAccess = template.CreatorId == doctorId
                || template.Visibility == VisibilityLevel.Hospital
                || (template.Visibility == VisibilityLevel.Department && doctor != null && template.DepartmentId == doctor.DepartmentId);

            if (!hasAccess) return (false, "Access denied", null);

            return (true, "Template retrieved", MapToDTO(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", templateId);
            return (false, "Error retrieving template", null);
        }
    }

    public Task<(bool Success, string Message, object? Data)> GetTemplateUsageStatsAsync(Guid templateId, Guid doctorId)
    {
        throw new NotImplementedException();
    }

    private TemplateDTO MapToDTO(Template t)
    {
        TemplateSchemaDTO schema = new();
        try {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            schema = JsonSerializer.Deserialize<TemplateSchemaDTO>(
                t.TemplateSchema,
                options
            ) ?? new();
        } catch(Exception ex) {
            _logger.LogWarning(ex, "Failed to deserialize schema for template {TemplateId}", t.Id);
        }

        return new TemplateDTO
        {
            Id = t.Id,
            TemplateName = t.TemplateName,
            Description = t.Description,
            CreatedBy = t.CreatorId,
            CreatedFromRecordId = t.CreatedFromRecordId,
            BasedOnTemplateId = t.BasedOnTemplateId,
            Visibility = t.Visibility,
            DepartmentId = t.DepartmentId,
            Schema = schema,
            Version = t.Version,
            IsActive = t.IsActive,
            UsageCount = t.UsageCount,
            LastUsedAt = t.LastUsedAt,
            AverageEntryTimeSeconds = t.AverageEntryTimeSeconds,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
