using TaskFlow.Core.Enums;

namespace TaskFlow.Application.DTOs.Project;

public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}