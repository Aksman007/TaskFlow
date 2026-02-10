using TaskFlow.Core.Documents;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface ITaskCommentRepository
{
    Task<TaskComment> AddCommentAsync(TaskComment comment);
    Task<TaskComment?> GetCommentByIdAsync(string id);
    Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(Guid taskId);
    Task UpdateCommentAsync(string id, string content);
    Task DeleteCommentAsync(string id);
    Task<int> GetTaskCommentCountAsync(Guid taskId);
    Task<(IEnumerable<TaskComment> Items, int TotalCount)> GetTaskCommentsPagedAsync(Guid taskId, int skip, int take);
}