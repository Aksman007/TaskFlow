using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Activity;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityController : BaseController
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        IActivityLogRepository activityLogRepository,
        IProjectRepository projectRepository,
        ILogger<ActivityController> logger)
    {
        _activityLogRepository = activityLogRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get activity logs for a project
    /// </summary>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetProjectActivity(
        Guid projectId,
        [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(projectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        var activities = await _activityLogRepository.GetProjectActivityAsync(projectId, limit);

        var dtos = activities.Select(a => new ActivityLogDto
        {
            Id = a.Id,
            ProjectId = a.ProjectId,
            UserId = a.UserId,
            UserName = a.UserName,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Metadata = a.Metadata,
            Timestamp = a.Timestamp
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Get activity logs for a specific task
    /// </summary>
    [HttpGet("task/{taskId}")]
    [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetTaskActivity(
        Guid taskId,
        [FromQuery] int limit = 50)
    {
        var activities = await _activityLogRepository.GetTaskActivityAsync(taskId, limit);

        var dtos = activities.Select(a => new ActivityLogDto
        {
            Id = a.Id,
            ProjectId = a.ProjectId,
            UserId = a.UserId,
            UserName = a.UserName,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Metadata = a.Metadata,
            Timestamp = a.Timestamp
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Get activity logs for the current user
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetMyActivity([FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        var activities = await _activityLogRepository.GetUserActivityAsync(userId, limit);

        var dtos = activities.Select(a => new ActivityLogDto
        {
            Id = a.Id,
            ProjectId = a.ProjectId,
            UserId = a.UserId,
            UserName = a.UserName,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Metadata = a.Metadata,
            Timestamp = a.Timestamp
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Get activity logs for a date range
    /// </summary>
    [HttpGet("project/{projectId}/range")]
    [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetActivityByDateRange(
        Guid projectId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(projectId, userId);

        if (!isMember)
        {
            return Forbid();
        }

        var activities = await _activityLogRepository.GetActivityByDateRangeAsync(
            projectId, startDate, endDate);

        var dtos = activities.Select(a => new ActivityLogDto
        {
            Id = a.Id,
            ProjectId = a.ProjectId,
            UserId = a.UserId,
            UserName = a.UserName,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Metadata = a.Metadata,
            Timestamp = a.Timestamp
        });

        return Ok(dtos);
    }
}