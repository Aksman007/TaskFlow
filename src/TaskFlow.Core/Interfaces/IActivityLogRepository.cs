using TaskFlow.Core.Documents;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface IActivityLogRepository
{
    Task LogActivityAsync(ActivityLog log);
    Task<IEnumerable<ActivityLog>> GetProjectActivityAsync(Guid projectId, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetUserActivityAsync(Guid userId, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetTaskActivityAsync(Guid taskId, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetActivityByDateRangeAsync(Guid projectId, DateTime startDate, DateTime endDate);
}