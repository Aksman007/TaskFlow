using System.ComponentModel.DataAnnotations;
using TaskFlow.Core.Enums;

namespace TaskFlow.Application.DTOs.Task;

public class UpdateTaskRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public Core.Enums.TaskStatus Status { get; set; }

    [Required]
    public TaskPriority Priority { get; set; }

    public Guid? AssignedToId { get; set; }

    public DateTime? DueDate { get; set; }
}