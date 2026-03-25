using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class ChatMessage : BaseEntity
{
    // Participants
    [Required]
    public Guid SenderId { get; set; }

    [Required]
    public Guid ReceiverId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SenderRole { get; set; } = string.Empty; // "Doctor" or "Patient"

    // Message Content
    [Required]
    public string MessageText { get; set; } = string.Empty;

    [MaxLength(50)]
    public string MessageType { get; set; } = "Text"; // "Text" (future: "Image", "File")

    // Read Status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Edit Tracking
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }

    // Soft delete is inherited from BaseEntity (IsDeleted, UpdatedAt)
    public DateTime? DeletedAt { get; set; }

    // Timestamps
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    // Optional: Link to a health record / consultation
    public Guid? RelatedHealthRecordId { get; set; }

    // Navigation Properties
    public ApplicationUser Sender { get; set; } = null!;
    public ApplicationUser Receiver { get; set; } = null!;
    public MedicalRecord? RelatedHealthRecord { get; set; }
}
