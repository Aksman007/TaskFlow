using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.API.Controllers;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Comment;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.UnitTests.Helpers;

namespace TaskFlow.UnitTests.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ITaskCommentRepository> _commentRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IHubContext<TaskHub>> _hubContextMock;
    private readonly Mock<ILogger<CommentsController>> _loggerMock;
    private readonly CommentsController _controller;
    private readonly Guid _currentUserId;

    public CommentsControllerTests()
    {
        _commentRepositoryMock = new Mock<ITaskCommentRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _hubContextMock = new Mock<IHubContext<TaskHub>>();
        _loggerMock = new Mock<ILogger<CommentsController>>();

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);

        _controller = new CommentsController(
            _commentRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);

        _currentUserId = Guid.NewGuid();
        TestHelpers.SetupControllerContext(_controller, _currentUserId, "test@example.com", "Test User");
    }

    #region GetTaskComments Tests

    [Fact]
    public async Task GetTaskComments_WhenUserIsMember_ReturnsComments()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var comments = new List<TaskComment>
        {
            CreateTestComment(taskId),
            CreateTestComment(taskId),
            CreateTestComment(taskId)
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(task.ProjectId, _currentUserId))
            .ReturnsAsync(true);

        _commentRepositoryMock
            .Setup(x => x.GetTaskCommentsAsync(taskId))
            .ReturnsAsync(comments);

        // Act
        var result = await _controller.GetTaskComments(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var commentDtos = okResult.Value.Should().BeAssignableTo<IEnumerable<CommentDto>>().Subject;
        commentDtos.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTaskComments_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.GetTaskComments(taskId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTaskComments_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(task.ProjectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetTaskComments(taskId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region AddComment Tests

    [Fact]
    public async Task AddComment_WithValidRequest_ReturnsComment()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var request = new AddCommentRequest
        {
            TaskId = taskId,
            ProjectId = projectId,
            Content = "This is a test comment"
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(task.ProjectId, _currentUserId))
            .ReturnsAsync(true);

        _commentRepositoryMock
            .Setup(x => x.AddCommentAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) => c);

        // Act
        var result = await _controller.AddComment(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var commentDto = okResult.Value.Should().BeOfType<CommentDto>().Subject;
        commentDto.Content.Should().Be(request.Content);
        commentDto.TaskId.Should().Be(taskId);
        commentDto.UserId.Should().Be(_currentUserId);
    }

    [Fact]
    public async Task AddComment_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new AddCommentRequest
        {
            TaskId = taskId,
            ProjectId = Guid.NewGuid(),
            Content = "Test comment"
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.AddComment(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddComment_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var request = new AddCommentRequest
        {
            TaskId = taskId,
            ProjectId = projectId,
            Content = "Test comment"
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(task.ProjectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AddComment(request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddComment_SendsSignalRNotification()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var request = new AddCommentRequest
        {
            TaskId = taskId,
            ProjectId = projectId,
            Content = "Test comment"
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(task.ProjectId, _currentUserId))
            .ReturnsAsync(true);

        _commentRepositoryMock
            .Setup(x => x.AddCommentAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) => c);

        // Act
        await _controller.AddComment(request);

        // Assert
        _hubContextMock.Verify(h => h.Clients.Group($"project_{projectId}"), Times.Once);
    }

    #endregion

    #region UpdateComment Tests

    [Fact]
    public async Task UpdateComment_WhenOwner_ReturnsUpdatedComment()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var comment = new TaskComment
        {
            Id = commentId,
            TaskId = taskId,
            UserId = _currentUserId,
            UserName = "Test User",
            Content = "Original content",
            CreatedAt = DateTime.UtcNow
        };

        var newContent = "Updated content";

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        _commentRepositoryMock
            .Setup(x => x.UpdateCommentAsync(commentId, newContent))
            .Returns(Task.CompletedTask);

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        var result = await _controller.UpdateComment(commentId, newContent);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var commentDto = okResult.Value.Should().BeOfType<CommentDto>().Subject;
        commentDto.Content.Should().Be(newContent);
    }

    [Fact]
    public async Task UpdateComment_WhenNotOwner_ReturnsForbid()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";
        var otherUserId = Guid.NewGuid();

        var comment = new TaskComment
        {
            Id = commentId,
            TaskId = Guid.NewGuid(),
            UserId = otherUserId, // Different user
            UserName = "Other User",
            Content = "Original content",
            CreatedAt = DateTime.UtcNow
        };

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _controller.UpdateComment(commentId, "New content");

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateComment_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync((TaskComment?)null);

        // Act
        var result = await _controller.UpdateComment(commentId, "New content");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteComment Tests

    [Fact]
    public async Task DeleteComment_WhenOwner_ReturnsNoContent()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var comment = new TaskComment
        {
            Id = commentId,
            TaskId = taskId,
            UserId = _currentUserId,
            UserName = "Test User",
            Content = "Content to delete",
            CreatedAt = DateTime.UtcNow
        };

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        _commentRepositoryMock
            .Setup(x => x.DeleteCommentAsync(commentId))
            .Returns(Task.CompletedTask);

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteComment_WhenNotOwner_ReturnsForbid()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";
        var otherUserId = Guid.NewGuid();

        var comment = new TaskComment
        {
            Id = commentId,
            TaskId = Guid.NewGuid(),
            UserId = otherUserId, // Different user
            UserName = "Other User",
            Content = "Content",
            CreatedAt = DateTime.UtcNow
        };

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteComment_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync((TaskComment?)null);

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteComment_SendsSignalRNotification()
    {
        // Arrange
        var commentId = "507f1f77bcf86cd799439011";
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(projectId: projectId);

        var comment = new TaskComment
        {
            Id = commentId,
            TaskId = taskId,
            UserId = _currentUserId,
            UserName = "Test User",
            Content = "Content",
            CreatedAt = DateTime.UtcNow
        };

        _commentRepositoryMock
            .Setup(x => x.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        _commentRepositoryMock
            .Setup(x => x.DeleteCommentAsync(commentId))
            .Returns(Task.CompletedTask);

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        await _controller.DeleteComment(commentId);

        // Assert
        _hubContextMock.Verify(h => h.Clients.Group($"project_{projectId}"), Times.Once);
    }

    #endregion

    #region Helper Methods

    private TaskComment CreateTestComment(Guid taskId, Guid? userId = null)
    {
        return new TaskComment
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TaskId = taskId,
            UserId = userId ?? Guid.NewGuid(),
            UserName = "Test User",
            Content = "Test comment content",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
