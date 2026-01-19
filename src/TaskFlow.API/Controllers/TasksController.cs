using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Task;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : BaseController
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHubContext<TaskHub> _hubContext;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IHubContext<TaskHub> hubContext,
        ILogger<TasksController> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDto>> GetTask(Guid id)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(id);

        if (task == null)
        {
            return NotFound(new { error = "Task not found" });
        }

        // Check if user has access to this task's project
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(task.ProjectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        return Ok(MapToDto(task));
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetProjectTasks(Guid projectId)
    {
        // Check if user is a member of the project
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(projectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        var tasks = await _taskRepository.GetProjectTasksAsync(projectId);
        return Ok(tasks.Select(MapToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verify user is a member of the project
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(request.ProjectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        // If assignedToId is provided, verify the user exists and is a member
        if (request.AssignedToId.HasValue)
        {
            var assignedUser = await _userRepository.GetByIdAsync(request.AssignedToId.Value);
            if (assignedUser == null)
            {
                return BadRequest(new { error = "Assigned user does not exist" });
            }

            var isAssigneeMember = await _projectRepository.IsUserMemberAsync(request.ProjectId, request.AssignedToId.Value);
            if (!isAssigneeMember)
            {
                return BadRequest(new { error = "Cannot assign task to a user who is not a project member" });
            }
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            ProjectId = request.ProjectId,
            AssignedToId = request.AssignedToId,
            Status = Core.Enums.TaskStatus.Todo,
            Priority = request.Priority,
            CreatedAt = DateTime.UtcNow,
            DueDate = request.DueDate.HasValue
                ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc)
                : null
        };

        await _taskRepository.CreateAsync(task);

        // Reload with navigation properties
        task = await _taskRepository.GetByIdWithDetailsAsync(task.Id);
        var taskDto = MapToDto(task!);

        // Notify via SignalR
        await _hubContext.Clients
            .Group($"project_{request.ProjectId}")
            .SendAsync("TaskCreated", taskDto);

        _logger.LogInformation("Task created: {TaskId} by user {UserId}", task.Id, userId);

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, taskDto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(new { error = "Task not found" });
        }

        // Verify user is a member of the project
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(task.ProjectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        // Track if status changed
        var statusChanged = task.Status != request.Status;
        var oldStatus = task.Status;

        // Update task properties
        task.Title = request.Title;
        task.Description = request.Description ?? string.Empty;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssignedToId = request.AssignedToId;
        task.DueDate = request.DueDate.HasValue
            ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc)
            : null;

        await _taskRepository.UpdateAsync(task);

        // Reload with navigation properties
        task = await _taskRepository.GetByIdWithDetailsAsync(id);
        var taskDto = MapToDto(task!);

        // Notify via SignalR
        await _hubContext.Clients
            .Group($"project_{task.ProjectId}")
            .SendAsync("TaskUpdated", taskDto);

        if (statusChanged)
        {
            await _hubContext.Clients
                .Group($"project_{task.ProjectId}")
                .SendAsync("TaskStatusChanged", new
                {
                    taskId = task.Id,
                    oldStatus = oldStatus.ToString(),
                    newStatus = task.Status.ToString(),
                    task = taskDto
                });
        }

        _logger.LogInformation("Task updated: {TaskId} by user {UserId}", task.Id, userId);

        return Ok(taskDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);

        if (task == null)
        {
            return NotFound(new { error = "Task not found" });
        }

        // Check permissions
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(task.ProjectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        var projectId = task.ProjectId;

        await _taskRepository.DeleteAsync(id);

        // Notify via SignalR
        await _hubContext.Clients
            .Group($"project_{projectId}")
            .SendAsync("TaskDeleted", id);

        _logger.LogInformation("Task deleted: {TaskId} by user {UserId}", id, userId);

        return NoContent();
    }

    // Helper method
    private TaskDto MapToDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            AssignedToId = task.AssignedToId,
            AssignedToName = task.AssignedTo?.FullName,
            Status = task.Status,
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate
        };
    }
}