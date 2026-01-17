using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TaskFlow.Application.DTOs.Comment;
using TaskFlow.Application.DTOs.Task;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Hubs;

[Authorize]
public class TaskHub : Hub
{
    private readonly IProjectRepository _projectRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ILogger<TaskHub> _logger;

    public TaskHub(
        IProjectRepository projectRepository,
        IActivityLogRepository activityLogRepository,
        ILogger<TaskHub> logger)
    {
        _projectRepository = projectRepository;
        _activityLogRepository = activityLogRepository;
        _logger = logger;
    }

    // Connection Management
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var userName = GetUserName();

        _logger.LogInformation("User connected: {UserId} - {UserName}", userId, userName);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        _logger.LogInformation("User disconnected: {UserId}", userId);

        if (exception != null)
        {
            _logger.LogError(exception, "User disconnected with error: {UserId}", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Group Management
    public async Task JoinProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var projectGuid))
        {
            _logger.LogWarning("Invalid project ID: {ProjectId}", projectId);
            return;
        }

        var userId = GetUserId();

        // Verify user is a member of the project
        var isMember = await _projectRepository.IsUserMemberAsync(projectGuid, userId);

        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to join project {ProjectId} without membership",
                userId, projectId);
            throw new HubException("You are not a member of this project");
        }

        var groupName = $"project_{projectId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("User {UserId} joined project group {ProjectId}", userId, projectId);

        // Notify others in the group
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", new
        {
            UserId = userId,
            UserName = GetUserName(),
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LeaveProject(string projectId)
    {
        var groupName = $"project_{projectId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        var userId = GetUserId();
        _logger.LogInformation("User {UserId} left project group {ProjectId}", userId, projectId);

        // Notify others
        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", new
        {
            UserId = userId,
            UserName = GetUserName(),
            Timestamp = DateTime.UtcNow
        });
    }

    // Task Events
    public async Task TaskCreated(TaskDto task)
    {
        var userId = GetUserId();
        var userName = GetUserName();

        // Log activity
        await _activityLogRepository.LogActivityAsync(new ActivityLog
        {
            ProjectId = task.ProjectId,
            UserId = userId,
            UserName = userName,
            Action = "created_task",
            EntityType = "task",
            EntityId = task.Id,
            Metadata = new Dictionary<string, object>
            {
                { "title", task.Title },
                { "status", task.Status.ToString() },
                { "priority", task.Priority.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        // Broadcast to project group
        var groupName = $"project_{task.ProjectId}";
        await Clients.Group(groupName).SendAsync("TaskCreated", task);

        _logger.LogInformation("Task created broadcast: {TaskId} in project {ProjectId}",
            task.Id, task.ProjectId);
    }

    public async Task TaskUpdated(TaskDto task)
    {
        var userId = GetUserId();
        var userName = GetUserName();

        // Log activity
        await _activityLogRepository.LogActivityAsync(new ActivityLog
        {
            ProjectId = task.ProjectId,
            UserId = userId,
            UserName = userName,
            Action = "updated_task",
            EntityType = "task",
            EntityId = task.Id,
            Metadata = new Dictionary<string, object>
            {
                { "title", task.Title },
                { "status", task.Status.ToString() },
                { "priority", task.Priority.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        // Broadcast to project group
        var groupName = $"project_{task.ProjectId}";
        await Clients.Group(groupName).SendAsync("TaskUpdated", task);

        _logger.LogInformation("Task updated broadcast: {TaskId} in project {ProjectId}",
            task.Id, task.ProjectId);
    }

    public async Task TaskDeleted(Guid projectId, Guid taskId)
    {
        var userId = GetUserId();
        var userName = GetUserName();

        // Log activity
        await _activityLogRepository.LogActivityAsync(new ActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            UserName = userName,
            Action = "deleted_task",
            EntityType = "task",
            EntityId = taskId,
            Metadata = new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        });

        // Broadcast to project group
        var groupName = $"project_{projectId}";
        await Clients.Group(groupName).SendAsync("TaskDeleted", taskId);

        _logger.LogInformation("Task deleted broadcast: {TaskId} in project {ProjectId}",
            taskId, projectId);
    }

    public async Task TaskStatusChanged(Guid projectId, Guid taskId, string oldStatus, string newStatus)
    {
        var userId = GetUserId();
        var userName = GetUserName();

        // Log activity
        await _activityLogRepository.LogActivityAsync(new ActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            UserName = userName,
            Action = "changed_task_status",
            EntityType = "task",
            EntityId = taskId,
            Metadata = new Dictionary<string, object>
            {
                { "oldStatus", oldStatus },
                { "newStatus", newStatus }
            },
            Timestamp = DateTime.UtcNow
        });

        // Broadcast to project group
        var groupName = $"project_{projectId}";
        await Clients.Group(groupName).SendAsync("TaskStatusChanged", new
        {
            TaskId = taskId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedBy = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    // Comment Events
    public async Task CommentAdded(Guid projectId, Guid taskId, CommentDto comment)
    {
        var userId = GetUserId();
        var userName = GetUserName();

        // Log activity
        await _activityLogRepository.LogActivityAsync(new ActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            UserName = userName,
            Action = "added_comment",
            EntityType = "comment",
            EntityId = taskId,
            Metadata = new Dictionary<string, object>
            {
                { "taskId", taskId.ToString() },
                { "commentPreview", comment.Content.Length > 50
                    ? comment.Content.Substring(0, 50) + "..."
                    : comment.Content }
            },
            Timestamp = DateTime.UtcNow
        });

        // Broadcast to project group
        var groupName = $"project_{projectId}";
        await Clients.Group(groupName).SendAsync("CommentAdded", taskId, comment);

        _logger.LogInformation("Comment added broadcast: Task {TaskId} in project {ProjectId}",
            taskId, projectId);
    }

    public async Task CommentUpdated(Guid projectId, Guid taskId, CommentDto comment)
    {
        var groupName = $"project_{projectId}";
        await Clients.Group(groupName).SendAsync("CommentUpdated", taskId, comment);
    }

    public async Task CommentDeleted(Guid projectId, Guid taskId, string commentId)
    {
        var groupName = $"project_{projectId}";
        await Clients.Group(groupName).SendAsync("CommentDeleted", taskId, commentId);
    }

    // User Activity (typing indicators, presence, etc.)
    public async Task UserTyping(Guid projectId, Guid taskId)
    {
        var userId = GetUserId();
        var userName = GetUserName();
        var groupName = $"project_{projectId}";

        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            TaskId = taskId,
            UserId = userId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task UserStoppedTyping(Guid projectId, Guid taskId)
    {
        var userId = GetUserId();
        var groupName = $"project_{projectId}";

        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
        {
            TaskId = taskId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    // Helper Methods
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new HubException("User ID not found in token");
        }
        return Guid.Parse(userIdClaim);
    }

    private string GetUserName()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? throw new HubException("User name not found in token");
    }

    private string GetUserEmail()
    {
        return Context.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new HubException("Email not found in token");
    }
}