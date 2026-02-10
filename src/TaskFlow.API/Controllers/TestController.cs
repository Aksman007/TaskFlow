using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.Core.Documents;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TestController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public TestController(
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        ITaskRepository taskRepository,
        IActivityLogRepository activityLogRepository)
    {
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _activityLogRepository = activityLogRepository;
    }

    [HttpGet("create-sample-data")]
    public async Task<IActionResult> CreateSampleData()
    {
        try
        {
            // Create a user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.CreateAsync(user);

            // Create a project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                Description = "A test project",
                OwnerId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _projectRepository.CreateAsync(project);

            // Create a task
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Test Task",
                Description = "A test task",
                ProjectId = project.Id,
                AssignedToId = user.Id,
                Status = Core.Enums.TaskStatus.Todo,
                Priority = Core.Enums.TaskPriority.High,
                CreatedAt = DateTime.UtcNow
            };
            await _taskRepository.CreateAsync(task);

            // Log activity
            await _activityLogRepository.LogActivityAsync(new ActivityLog
            {
                ProjectId = project.Id,
                UserId = user.Id,
                UserName = user.FullName,
                Action = "created_task",
                EntityType = "task",
                EntityId = task.Id,
                Metadata = new Dictionary<string, object>
                {
                    { "title", task.Title }
                },
                Timestamp = DateTime.UtcNow
            });

            return Ok(new
            {
                message = "Sample data created successfully!",
                user = new { user.Id, user.Email, user.FullName },
                project = new { project.Id, project.Name },
                task = new { task.Id, task.Title }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var user = await _userRepository.GetByEmailAsync("test@example.com");
        return Ok(user);
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] string email = "test@example.com")
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return NotFound("User not found");

        var projects = await _projectRepository.GetUserProjectsAsync(user.Id);
        return Ok(projects);
    }

    [HttpGet("tasks/{projectId}")]
    public async Task<IActionResult> GetProjectTasks(Guid projectId)
    {
        var tasks = await _taskRepository.GetProjectTasksAsync(projectId);
        return Ok(tasks);
    }

    [HttpGet("activity/{projectId}")]
    public async Task<IActionResult> GetProjectActivity(Guid projectId)
    {
        var activity = await _activityLogRepository.GetProjectActivityAsync(projectId);
        return Ok(activity);
    }
}