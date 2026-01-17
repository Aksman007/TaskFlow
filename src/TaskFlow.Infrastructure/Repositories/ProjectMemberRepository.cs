using Microsoft.EntityFrameworkCore;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly TaskFlowDbContext _context;

    public ProjectMemberRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectMember?> GetByIdAsync(Guid id)
    {
        return await _context.ProjectMembers
            .AsNoTracking()
            .Include(pm => pm.User)
            .Include(pm => pm.Project)
            .FirstOrDefaultAsync(pm => pm.Id == id);
    }

    public async Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .AsNoTracking()
            .Include(pm => pm.User)
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId)
    {
        return await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == projectId)
            .Include(pm => pm.User)
            .OrderBy(pm => pm.JoinedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectMember>> GetUserMembershipsAsync(Guid userId)
    {
        return await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.UserId == userId)
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Owner)
            .OrderByDescending(pm => pm.JoinedAt)
            .ToListAsync();
    }

    public async Task<ProjectMember> AddMemberAsync(ProjectMember member)
    {
        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task UpdateMemberRoleAsync(Guid id, ProjectRole role)
    {
        var member = await _context.ProjectMembers.FindAsync(id);
        if (member != null)
        {
            member.Role = role;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberAsync(Guid id)
    {
        var member = await _context.ProjectMembers.FindAsync(id);
        if (member != null)
        {
            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsMemberAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<ProjectRole?> GetUserRoleAsync(Guid projectId, Guid userId)
    {
        var member = await _context.ProjectMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        
        return member?.Role;
    }
}