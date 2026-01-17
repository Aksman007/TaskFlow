namespace TaskFlow.Core.Interfaces;

using TaskFlow.Core.Documents;

public interface IActivityLogRepository
{
    Task LogActivityAsync(ActivityLog log);
    Task<IEnumerable<ActivityLog>> GetProjectActivityAsync(Guid projectId, int limit = 50);
}