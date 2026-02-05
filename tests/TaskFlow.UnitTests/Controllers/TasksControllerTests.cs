using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.API.Controllers;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Task;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.UnitTests.Helpers;

namespace TaskFlow.UnitTests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IHubContext<TaskHub>> _hubContextMock;
    private readonly Mock<ILogger<TasksController>> _loggerMock;
    private readonly TasksController _controller;
    private readonly Guid _currentUserId;

    public TasksControllerTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _hubContextMock = new Mock<IHubContext<TaskHub>>();
        _loggerMock = new Mock<ILogger<TasksController>>();

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);

        _controller = new TasksController(
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _userRepositoryMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);

        _currentUserId = Guid.NewGuid();
        TestHelpers.SetupControllerContext(_controller, _currentUserId);
    }

    #region GetTask Tests

    [Fact]
    public async Task GetTask_WhenUserIsMemberAndTaskExists_ReturnsTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId);

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var taskDto = okResult.Value.Should().BeOfType<TaskDto>().Subject;
        taskDto.Id.Should().Be(taskId);
    }

    [Fact]
    public async Task GetTask_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTask_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId);

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region GetProjectTasks Tests

    [Fact]
    public async Task GetProjectTasks_WhenUserIsMember_ReturnsTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new List<TaskItem>
        {
            TestHelpers.CreateTestTask(projectId: projectId),
            TestHelpers.CreateTestTask(projectId: projectId),
            TestHelpers.CreateTestTask(projectId: projectId)
        };

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.GetProjectTasksAsync(projectId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetProjectTasks(projectId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var taskDtos = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskDto>>().Subject;
        taskDtos.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProjectTasks_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetProjectTasks(projectId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateTask Tests

    [Fact]
    public async Task CreateTask_WithValidRequest_ReturnsCreatedTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Task description",
            ProjectId = projectId,
            Priority = TaskPriority.High
        };

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync((TaskItem t) => t);

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new TaskItem
            {
                Id = id,
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                ProjectId = projectId,
                Status = Core.Enums.TaskStatus.Todo,
                Priority = request.Priority,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var taskDto = createdResult.Value.Should().BeOfType<TaskDto>().Subject;
        taskDto.Title.Should().Be(request.Title);
        taskDto.Status.Should().Be(Core.Enums.TaskStatus.Todo);
    }

    [Fact]
    public async Task CreateTask_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            ProjectId = projectId,
            Priority = TaskPriority.Medium
        };

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateTask_WithAssignee_ValidatesMembership()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var assignee = TestHelpers.CreateTestUser(assigneeId);

        var request = new CreateTaskRequest
        {
            Title = "New Task",
            ProjectId = projectId,
            AssignedToId = assigneeId,
            Priority = TaskPriority.Medium
        };

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(assigneeId))
            .ReturnsAsync(assignee);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, assigneeId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync((TaskItem t) => t);

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new TaskItem
            {
                Id = id,
                Title = request.Title,
                ProjectId = projectId,
                AssignedToId = assigneeId,
                AssignedTo = assignee,
                Status = Core.Enums.TaskStatus.Todo,
                Priority = request.Priority,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var taskDto = createdResult.Value.Should().BeOfType<TaskDto>().Subject;
        taskDto.AssignedToId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task CreateTask_WithNonExistentAssignee_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var request = new CreateTaskRequest
        {
            Title = "New Task",
            ProjectId = projectId,
            AssignedToId = assigneeId,
            Priority = TaskPriority.Medium
        };

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(assigneeId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { error = "Assigned user does not exist" });
    }

    [Fact]
    public async Task CreateTask_WithNonMemberAssignee_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var assignee = TestHelpers.CreateTestUser(assigneeId);

        var request = new CreateTaskRequest
        {
            Title = "New Task",
            ProjectId = projectId,
            AssignedToId = assigneeId,
            Priority = TaskPriority.Medium
        };

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(assigneeId))
            .ReturnsAsync(assignee);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, assigneeId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { error = "Cannot assign task to a user who is not a project member" });
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async Task UpdateTask_WithValidRequest_ReturnsUpdatedTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId);

        var request = new UpdateTaskRequest
        {
            Title = "Updated Task",
            Description = "Updated description",
            Status = Core.Enums.TaskStatus.InProgress,
            Priority = TaskPriority.High
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<TaskItem>()))
            .Returns(Task.CompletedTask);

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(taskId))
            .ReturnsAsync(new TaskItem
            {
                Id = taskId,
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                ProjectId = projectId,
                Status = request.Status,
                Priority = request.Priority,
                CreatedAt = task.CreatedAt
            });

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var taskDto = okResult.Value.Should().BeOfType<TaskDto>().Subject;
        taskDto.Title.Should().Be(request.Title);
        taskDto.Status.Should().Be(Core.Enums.TaskStatus.InProgress);
    }

    [Fact]
    public async Task UpdateTask_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "Updated Task",
            Status = Core.Enums.TaskStatus.InProgress,
            Priority = TaskPriority.High
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTask_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId);

        var request = new UpdateTaskRequest
        {
            Title = "Updated Task",
            Status = Core.Enums.TaskStatus.InProgress,
            Priority = TaskPriority.High
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateTask_StatusChange_SendsSignalRNotification()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId, status: Core.Enums.TaskStatus.Todo);

        var request = new UpdateTaskRequest
        {
            Title = task.Title,
            Status = Core.Enums.TaskStatus.Done,
            Priority = task.Priority
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<TaskItem>()))
            .Returns(Task.CompletedTask);

        _taskRepositoryMock
            .Setup(x => x.GetByIdWithDetailsAsync(taskId))
            .ReturnsAsync(new TaskItem
            {
                Id = taskId,
                Title = request.Title,
                ProjectId = projectId,
                Status = request.Status,
                Priority = request.Priority,
                CreatedAt = task.CreatedAt
            });

        // Act
        await _controller.UpdateTask(taskId, request);

        // Assert - verify SignalR notification was sent for status change
        _hubContextMock.Verify(h => h.Clients.Group($"project_{projectId}"), Times.AtLeast(2)); // TaskUpdated + TaskStatusChanged
    }

    #endregion

    #region DeleteTask Tests

    [Fact]
    public async Task DeleteTask_WhenUserIsMember_ReturnsNoContent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId);

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.DeleteAsync(taskId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteTask(taskId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTask_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.DeleteTask(taskId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteTask_WhenUserNotMember_ReturnsForbid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TestHelpers.CreateTestTask(taskId, projectId: projectId);

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTask(taskId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
