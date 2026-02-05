using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;

namespace TaskFlow.UnitTests.Helpers;

public static class TestHelpers
{
    public static User CreateTestUser(
        Guid? id = null,
        string? email = null,
        string? fullName = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email ?? $"user{Guid.NewGuid():N}@test.com",
            FullName = fullName ?? "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Project CreateTestProject(
        Guid? id = null,
        string? name = null,
        Guid? ownerId = null,
        User? owner = null)
    {
        var projectOwner = owner ?? CreateTestUser(ownerId);
        return new Project
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "Test Project",
            Description = "Test project description",
            OwnerId = projectOwner.Id,
            Owner = projectOwner,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ProjectMember CreateTestProjectMember(
        Guid? id = null,
        Guid? projectId = null,
        Guid? userId = null,
        User? user = null,
        ProjectRole role = ProjectRole.Member)
    {
        var memberUser = user ?? CreateTestUser(userId);
        return new ProjectMember
        {
            Id = id ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            UserId = memberUser.Id,
            User = memberUser,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    public static TaskItem CreateTestTask(
        Guid? id = null,
        string? title = null,
        Guid? projectId = null,
        Guid? assignedToId = null,
        User? assignedTo = null,
        Core.Enums.TaskStatus status = Core.Enums.TaskStatus.Todo,
        TaskPriority priority = TaskPriority.Medium)
    {
        return new TaskItem
        {
            Id = id ?? Guid.NewGuid(),
            Title = title ?? "Test Task",
            Description = "Test task description",
            ProjectId = projectId ?? Guid.NewGuid(),
            AssignedToId = assignedToId,
            AssignedTo = assignedTo,
            Status = status,
            Priority = priority,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ClaimsPrincipal CreateClaimsPrincipal(
        Guid userId,
        string email = "test@example.com",
        string name = "Test User")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    public static void SetupControllerContext(
        ControllerBase controller,
        Guid userId,
        string email = "test@example.com",
        string name = "Test User")
    {
        var claimsPrincipal = CreateClaimsPrincipal(userId, email, name);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }
}
