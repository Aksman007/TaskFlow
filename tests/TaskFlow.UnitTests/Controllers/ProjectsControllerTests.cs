using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.API.Controllers;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Project;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.UnitTests.Helpers;

namespace TaskFlow.UnitTests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IProjectMemberRepository> _memberRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly Mock<IHubContext<TaskHub>> _hubContextMock;
    private readonly Mock<ILogger<ProjectsController>> _loggerMock;
    private readonly ProjectsController _controller;
    private readonly Guid _currentUserId;

    public ProjectsControllerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _memberRepositoryMock = new Mock<IProjectMemberRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _hubContextMock = new Mock<IHubContext<TaskHub>>();
        _loggerMock = new Mock<ILogger<ProjectsController>>();

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);

        _controller = new ProjectsController(
            _projectRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _userRepositoryMock.Object,
            _activityLogRepositoryMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);

        _currentUserId = Guid.NewGuid();
        TestHelpers.SetupControllerContext(_controller, _currentUserId);
    }

    #region GetUserProjects Tests

    [Fact]
    public async Task GetUserProjects_ReturnsUserProjects()
    {
        // Arrange
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var projects = new List<Project>
        {
            TestHelpers.CreateTestProject(owner: owner),
            TestHelpers.CreateTestProject(owner: owner)
        };

        _projectRepositoryMock
            .Setup(x => x.GetUserProjectsPagedAsync(_currentUserId, 0, 20))
            .ReturnsAsync((projects.AsEnumerable(), projects.Count));

        _memberRepositoryMock
            .Setup(x => x.GetProjectMembersAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ProjectMember> { TestHelpers.CreateTestProjectMember(user: owner) });

        _taskRepositoryMock
            .Setup(x => x.GetProjectTaskCountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.GetUserProjects();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserProjects_WhenNoProjects_ReturnsEmptyList()
    {
        // Arrange
        _projectRepositoryMock
            .Setup(x => x.GetUserProjectsPagedAsync(_currentUserId, 0, 20))
            .ReturnsAsync((Enumerable.Empty<Project>(), 0));

        // Act
        var result = await _controller.GetUserProjects();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion

    #region GetProject Tests

    [Fact]
    public async Task GetProject_WhenUserIsMember_ReturnsProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser();
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _memberRepositoryMock
            .Setup(x => x.GetProjectMembersAsync(projectId))
            .ReturnsAsync(new List<ProjectMember>());

        _taskRepositoryMock
            .Setup(x => x.GetProjectTaskCountAsync(projectId))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.GetProject(projectId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var projectDto = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
        projectDto.Id.Should().Be(projectId);
    }

    [Fact]
    public async Task GetProject_WhenUserIsNotMember_ReturnsForbid()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetProject(projectId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetProject_WhenProjectNotFound_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(x => x.IsUserMemberAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _controller.GetProject(projectId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateProject Tests

    [Fact]
    public async Task CreateProject_WithValidRequest_ReturnsCreatedProject()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "New Project",
            Description = "Project description"
        };

        var owner = TestHelpers.CreateTestUser(_currentUserId);

        _projectRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) => p);

        _memberRepositoryMock
            .Setup(x => x.AddMemberAsync(It.IsAny<ProjectMember>()))
            .ReturnsAsync((ProjectMember m) => m);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Project
            {
                Id = id,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                OwnerId = _currentUserId,
                Owner = owner,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _controller.CreateProject(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var projectDto = createdResult.Value.Should().BeOfType<ProjectDto>().Subject;
        projectDto.Name.Should().Be(request.Name);
        projectDto.OwnerId.Should().Be(_currentUserId);
    }

    [Fact]
    public async Task CreateProject_AddsCreatorAsAdminMember()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "New Project"
        };

        ProjectMember? capturedMember = null;
        var owner = TestHelpers.CreateTestUser(_currentUserId);

        _projectRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) => p);

        _memberRepositoryMock
            .Setup(x => x.AddMemberAsync(It.IsAny<ProjectMember>()))
            .Callback<ProjectMember>(m => capturedMember = m)
            .ReturnsAsync((ProjectMember m) => m);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Project
            {
                Id = id,
                Name = request.Name,
                OwnerId = _currentUserId,
                Owner = owner,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        await _controller.CreateProject(request);

        // Assert
        capturedMember.Should().NotBeNull();
        capturedMember!.UserId.Should().Be(_currentUserId);
        capturedMember.Role.Should().Be(ProjectRole.Admin);
    }

    #endregion

    #region UpdateProject Tests

    [Fact]
    public async Task UpdateProject_WhenOwner_ReturnsUpdatedProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);

        var request = new UpdateProjectRequest
        {
            Name = "Updated Name",
            Description = "Updated description"
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _projectRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        _memberRepositoryMock
            .Setup(x => x.GetProjectMembersAsync(projectId))
            .ReturnsAsync(new List<ProjectMember>());

        _taskRepositoryMock
            .Setup(x => x.GetProjectTaskCountAsync(projectId))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.UpdateProject(projectId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var projectDto = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
        projectDto.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task UpdateProject_WhenNotOwnerOrAdmin_ReturnsForbid()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser();
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);

        var request = new UpdateProjectRequest
        {
            Name = "Updated Name"
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Member);

        // Act
        var result = await _controller.UpdateProject(projectId, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateProject_WhenProjectNotFound_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new UpdateProjectRequest { Name = "Updated" };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _controller.UpdateProject(projectId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteProject Tests

    [Fact]
    public async Task DeleteProject_WhenOwner_ReturnsNoContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _projectRepositoryMock
            .Setup(x => x.DeleteAsync(projectId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProject(projectId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProject_WhenNotOwner_ReturnsForbid()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser();
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProject(projectId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region AddMember Tests

    [Fact]
    public async Task AddMember_WhenOwnerAddsNewMember_ReturnsCreated()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var newUser = TestHelpers.CreateTestUser(email: "newmember@test.com");

        var request = new AddMemberRequest
        {
            Email = newUser.Email,
            Role = ProjectRole.Member
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(newUser.Email))
            .ReturnsAsync(newUser);

        _memberRepositoryMock
            .Setup(x => x.GetByProjectAndUserAsync(projectId, newUser.Id))
            .ReturnsAsync((ProjectMember?)null);

        _memberRepositoryMock
            .Setup(x => x.AddMemberAsync(It.IsAny<ProjectMember>()))
            .ReturnsAsync((ProjectMember m) => m);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new ProjectMember
            {
                Id = id,
                ProjectId = projectId,
                UserId = newUser.Id,
                User = newUser,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            });

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var memberDto = createdResult.Value.Should().BeOfType<ProjectMemberDto>().Subject;
        memberDto.UserEmail.Should().Be(newUser.Email);
    }

    [Fact]
    public async Task AddMember_WhenUserAlreadyMember_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var existingMember = TestHelpers.CreateTestUser();

        var request = new AddMemberRequest
        {
            Email = existingMember.Email,
            Role = ProjectRole.Member
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(existingMember.Email))
            .ReturnsAsync(existingMember);

        _memberRepositoryMock
            .Setup(x => x.GetByProjectAndUserAsync(projectId, existingMember.Id))
            .ReturnsAsync(TestHelpers.CreateTestProjectMember(projectId: projectId, user: existingMember));

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { error = "User is already a member of this project" });
    }

    [Fact]
    public async Task AddMember_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);

        var request = new AddMemberRequest
        {
            Email = "nonexistent@test.com",
            Role = ProjectRole.Member
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { error = "User not found with that email" });
    }

    #endregion

    #region UpdateMemberRole Tests

    [Fact]
    public async Task UpdateMemberRole_WhenOwner_UpdatesRole()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var memberUser = TestHelpers.CreateTestUser();
        var member = TestHelpers.CreateTestProjectMember(memberId, projectId, user: memberUser, role: ProjectRole.Member);

        var request = new UpdateMemberRoleRequest { Role = ProjectRole.Admin };

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _memberRepositoryMock
            .Setup(x => x.UpdateMemberRoleAsync(memberId, request.Role))
            .Returns(Task.CompletedTask);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateMemberRole(projectId, memberId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _memberRepositoryMock.Verify(x => x.UpdateMemberRoleAsync(memberId, request.Role), Times.Once);
    }

    [Fact]
    public async Task UpdateMemberRole_CannotChangeOwnerRole_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(ownerId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var ownerMember = TestHelpers.CreateTestProjectMember(projectId: projectId, user: owner, role: ProjectRole.Admin);

        var request = new UpdateMemberRoleRequest { Role = ProjectRole.Member };

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(ownerMember.Id))
            .ReturnsAsync(ownerMember);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        var result = await _controller.UpdateMemberRole(projectId, ownerMember.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { error = "Cannot change project owner's role" });
    }

    #endregion

    #region RemoveMember Tests

    [Fact]
    public async Task RemoveMember_WhenOwnerRemovesMember_ReturnsNoContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(_currentUserId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var memberUser = TestHelpers.CreateTestUser();
        var member = TestHelpers.CreateTestProjectMember(memberId, projectId, user: memberUser);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _memberRepositoryMock
            .Setup(x => x.RemoveMemberAsync(memberId))
            .Returns(Task.CompletedTask);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveMember_MemberRemovesSelf_ReturnsNoContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser();
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var currentUser = TestHelpers.CreateTestUser(_currentUserId);
        var member = TestHelpers.CreateTestProjectMember(memberId, projectId, user: currentUser);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Member);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _memberRepositoryMock
            .Setup(x => x.RemoveMemberAsync(memberId))
            .Returns(Task.CompletedTask);

        _activityLogRepositoryMock
            .Setup(x => x.LogActivityAsync(It.IsAny<ActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveMember_CannotRemoveOwner_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var owner = TestHelpers.CreateTestUser(ownerId);
        var project = TestHelpers.CreateTestProject(projectId, owner: owner);
        var ownerMember = TestHelpers.CreateTestProjectMember(projectId: projectId, user: owner);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(ownerMember.Id))
            .ReturnsAsync(ownerMember);

        _projectRepositoryMock
            .Setup(x => x.IsUserOwnerAsync(projectId, _currentUserId))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetUserRoleAsync(projectId, _currentUserId))
            .ReturnsAsync(ProjectRole.Admin);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        var result = await _controller.RemoveMember(projectId, ownerMember.Id);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { error = "Cannot remove project owner" });
    }

    #endregion
}
