namespace TaskFlow.Application.DTOs.Activity;

public class ActivityLogDto
{
    public string Id { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; }
}