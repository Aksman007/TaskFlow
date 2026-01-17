namespace TaskFlow.Core.Interfaces;

using TaskFlow.Core.Documents;

public interface ITaskCommentRepository
{
    Task<TaskComment> AddCommentAsync(TaskComment comment);
    Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(Guid taskId);
    Task UpdateCommentAsync(string id, string content);
    Task DeleteCommentAsync(string id);
}