using TaskFlow.Core.Entities;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project?> GetByIdWithMembersAsync(Guid id);
    Task<Project?> GetByIdWithTasksAsync(Guid id);
    Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId);
    Task<Project> CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
    Task<bool> IsUserMemberAsync(Guid projectId, Guid userId);
    Task<bool> IsUserOwnerAsync(Guid projectId, Guid userId);
    Task<(IEnumerable<Project> Items, int TotalCount)> GetUserProjectsPagedAsync(Guid userId, int skip, int take);
}