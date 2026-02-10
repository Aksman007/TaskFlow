using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<TaskItem>> GetProjectTasksAsync(Guid projectId);
    Task<IEnumerable<TaskItem>> GetUserTasksAsync(Guid userId);
    Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(Guid projectId, Enums.TaskStatus status);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
    Task<int> GetProjectTaskCountAsync(Guid projectId);
    Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetProjectTasksPagedAsync(Guid projectId, int skip, int take);
    Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetUserTasksPagedAsync(Guid userId, int skip, int take);
}