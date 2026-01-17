
namespace TaskFlow.Core.Interfaces;

using TaskFlow.Core.Entities;

public interface ITaskRepository
{
    Task<TaskItem> GetByIdAsync(Guid id);
    Task<IEnumerable<TaskItem>> GetProjectTasksAsync(Guid projectId);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
}