using TaskFlow.Core.Enums;

namespace TaskFlow.Core.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid? AssignedToId { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User? AssignedTo { get; set; }
}