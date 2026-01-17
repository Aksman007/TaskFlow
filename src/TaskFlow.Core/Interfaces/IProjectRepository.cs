namespace TaskFlow.Core.Interfaces;

using TaskFlow.Core.Entities;
public interface IProjectRepository
{
    Task<Project> GetByIdAsync(Guid id);
    Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId);
    Task<Project> CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task<bool> IsUserMemberAsync(Guid projectId, Guid userId);
}