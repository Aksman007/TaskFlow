using Microsoft.EntityFrameworkCore;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskFlowDbContext _context;

    public TaskRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.AssignedTo)
            .Include(t => t.Project)
                .ThenInclude(p => p.Owner)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<TaskItem>> GetProjectTasksAsync(Guid projectId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetUserTasksAsync(Guid userId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.AssignedToId == userId)
            .Include(t => t.Project)
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(Guid projectId, Core.Enums.TaskStatus status)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.Status == status)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        // Reload with navigation properties
        await _context.Entry(task)
            .Reference(t => t.AssignedTo)
            .LoadAsync();
        
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetProjectTaskCountAsync(Guid projectId)
    {
        return await _context.Tasks
            .CountAsync(t => t.ProjectId == projectId);
    }
}