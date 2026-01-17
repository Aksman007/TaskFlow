using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface IProjectMemberRepository
{
    Task<ProjectMember?> GetByIdAsync(Guid id);
    Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId);
    Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId);
    Task<IEnumerable<ProjectMember>> GetUserMembershipsAsync(Guid userId);
    Task<ProjectMember> AddMemberAsync(ProjectMember member);
    Task UpdateMemberRoleAsync(Guid id, ProjectRole role);
    Task RemoveMemberAsync(Guid id);
    Task<bool> IsMemberAsync(Guid projectId, Guid userId);
    Task<ProjectRole?> GetUserRoleAsync(Guid projectId, Guid userId);
}