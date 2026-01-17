using TaskFlow.Core.Enums;

namespace TaskFlow.Core.Entities;

public class ProjectMember
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}