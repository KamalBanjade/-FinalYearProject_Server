using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class TemplateVersionHistory : BaseEntity
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public int Version { get; set; }

    [Required]
    public ChangeType ChangeType { get; set; }

    [MaxLength(500)]
    public string? ChangeDescription { get; set; }

    [Required]
    public Guid ModifiedBy { get; set; }

    [Required]
    public Guid ModifierId { get; set; }

    public string? PreviousSchema { get; set; }

    [Required]
    public string NewSchema { get; set; } = string.Empty;

    [Required]
    public DateTime ChangedAt { get; set; }

    // Navigation Properties
    public Template Template { get; set; } = null!;
    public ApplicationUser Modifier { get; set; } = null!;
}
