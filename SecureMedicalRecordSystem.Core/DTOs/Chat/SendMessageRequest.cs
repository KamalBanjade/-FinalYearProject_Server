using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Chat;

public class SendMessageRequest
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string MessageText { get; set; } = string.Empty;
}
