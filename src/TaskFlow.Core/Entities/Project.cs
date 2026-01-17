namespace TaskFlow.Core.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<TaskItem> Tasks { get; set; } = [];
}