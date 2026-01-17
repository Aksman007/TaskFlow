using TaskFlow.Core.Entities;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<User>> GetProjectMembersAsync(Guid projectId);
    Task<bool> ExistsAsync(Guid id);
}