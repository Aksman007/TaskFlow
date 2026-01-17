namespace TaskFlow.Core.Interfaces;

using TaskFlow.Core.Entities;
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task<User> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<IEnumerable<User>> GetProjectMembersAsync(Guid projectId);
}