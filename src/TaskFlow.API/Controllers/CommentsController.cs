using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Comment;
using TaskFlow.Application.Helpers;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : BaseController
{
    private readonly ITaskCommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IHubContext<TaskHub> _hubContext;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        ITaskCommentRepository commentRepository,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IHubContext<TaskHub> hubContext,
        ILogger<CommentsController> logger)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("task/{taskId}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetTaskComments(Guid taskId)
    {
        // Verify task exists and user has access
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { error = "Task not found" });
        }

        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(task.ProjectId, userId);
        if (!isMember)
        {
            return Forbid();
        }

        var comments = await _commentRepository.GetTaskCommentsAsync(taskId);
        return Ok(comments.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> AddComment([FromBody] AddCommentRequest request)
    {
        // Verify task exists and user has access
        var task = await _taskRepository.GetByIdAsync(request.TaskId);
        if (task == null)
        {
            return NotFound(new { error = "Task not found" });
        }

        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(task.ProjectId, userId);
        if (!isMember)
        {
            return Forbid();
        }

        var comment = new TaskComment
        {
            TaskId = request.TaskId,
            UserId = userId,
            UserName = GetCurrentUserName(),
            Content = InputSanitizer.SanitizeHtml(request.Content),
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddCommentAsync(comment);

        var commentDto = MapToDto(comment);

        // Notify via SignalR
        await _hubContext.Clients
            .Group($"project_{request.ProjectId}")
            .SendAsync("CommentAdded", request.TaskId, commentDto);

        _logger.LogInformation("Comment added to task {TaskId} by user {UserId}",
            request.TaskId, userId);

        return Ok(commentDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CommentDto>> UpdateComment(string id, [FromBody] string content)
    {
        var comment = await _commentRepository.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound(new { error = "Comment not found" });
        }

        // Verify ownership
        var userId = GetCurrentUserId();
        if (comment.UserId != userId)
        {
            return Forbid();
        }

        var sanitizedContent = InputSanitizer.SanitizeHtml(content);
        await _commentRepository.UpdateCommentAsync(id, sanitizedContent);

        comment.Content = sanitizedContent;
        comment.UpdatedAt = DateTime.UtcNow;

        var commentDto = MapToDto(comment);

        // Get project ID for notification
        var task = await _taskRepository.GetByIdAsync(comment.TaskId);
        if (task != null)
        {
            await _hubContext.Clients
                .Group($"project_{task.ProjectId}")
                .SendAsync("CommentUpdated", comment.TaskId, commentDto);
        }

        return Ok(commentDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(string id)
    {
        var comment = await _commentRepository.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound(new { error = "Comment not found" });
        }

        // Verify ownership
        var userId = GetCurrentUserId();
        if (comment.UserId != userId)
        {
            return Forbid();
        }

        var taskId = comment.TaskId;

        await _commentRepository.DeleteCommentAsync(id);

        // Get project ID for notification
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task != null)
        {
            await _hubContext.Clients
                .Group($"project_{task.ProjectId}")
                .SendAsync("CommentDeleted", taskId, id);
        }

        _logger.LogInformation("Comment deleted: {CommentId} by user {UserId}", id, userId);

        return NoContent();
    }

    private CommentDto MapToDto(TaskComment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = comment.UserName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}