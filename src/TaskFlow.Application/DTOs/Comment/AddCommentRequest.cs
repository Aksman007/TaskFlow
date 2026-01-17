using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Application.DTOs.Comment;

public class AddCommentRequest
{
    [Required]
    public Guid TaskId { get; set; }

    [Required]
    public Guid ProjectId { get; set; } // Needed for SignalR group notification

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}