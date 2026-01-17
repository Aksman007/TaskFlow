using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Project;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Enums;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : BaseController
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IHubContext<TaskHub> _hubContext;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IActivityLogRepository activityLogRepository,
        IHubContext<TaskHub> hubContext,
        ILogger<ProjectsController> logger)
    {
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _activityLogRepository = activityLogRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetUserProjects()
    {
        var userId = GetCurrentUserId();
        var projects = await _projectRepository.GetUserProjectsAsync(userId);

        var projectDtos = new List<ProjectDto>();
        foreach (var project in projects)
        {
            var members = await _memberRepository.GetProjectMembersAsync(project.Id);
            var taskCount = await _taskRepository.GetProjectTaskCountAsync(project.Id);

            projectDtos.Add(new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                OwnerId = project.OwnerId,
                OwnerName = project.Owner.FullName,
                CreatedAt = project.CreatedAt,
                MemberCount = members.Count(),
                TaskCount = taskCount
            });
        }

        return Ok(projectDtos);
    }

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDto>> GetProject(Guid id)
    {
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(id, userId);

        if (!isMember)
        {
            return Forbid();
        }

        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound(new { error = "Project not found" });
        }

        var members = await _memberRepository.GetProjectMembersAsync(id);
        var taskCount = await _taskRepository.GetProjectTaskCountAsync(id);

        var dto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            OwnerId = project.OwnerId,
            OwnerName = project.Owner.FullName,
            CreatedAt = project.CreatedAt,
            MemberCount = members.Count(),
            TaskCount = taskCount
        };

        return Ok(dto);
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.CreateAsync(project);

        // Add creator as admin member
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        await _memberRepository.AddMemberAsync(member);

        // Log activity
        await _activityLogRepository.LogActivityAsync(new Core.Documents.ActivityLog
        {
            ProjectId = project.Id,
            UserId = userId,
            UserName = userName,
            Action = "created_project",
            EntityType = "project",
            EntityId = project.Id,
            Metadata = new Dictionary<string, object>
            {
                { "projectName", project.Name }
            },
            Timestamp = DateTime.UtcNow
        });

        // Reload project with owner
        project = await _projectRepository.GetByIdAsync(project.Id);

        var dto = new ProjectDto
        {
            Id = project!.Id,
            Name = project.Name,
            Description = project.Description,
            OwnerId = project.OwnerId,
            OwnerName = project.Owner.FullName,
            CreatedAt = project.CreatedAt,
            MemberCount = 1,
            TaskCount = 0
        };

        _logger.LogInformation("Project created: {ProjectId} by user {UserId}", project.Id, userId);

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, dto);
    }

    /// <summary>
    /// Update a project
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDto>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound(new { error = "Project not found" });
        }

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        // Check if user is owner or admin
        var isOwner = await _projectRepository.IsUserOwnerAsync(id, userId);
        var userRole = await _memberRepository.GetUserRoleAsync(id, userId);

        if (!isOwner && userRole != ProjectRole.Admin)
        {
            return Forbid();
        }

        project.Name = request.Name;
        project.Description = request.Description ?? string.Empty;

        await _projectRepository.UpdateAsync(project);

        // Log activity
        await _activityLogRepository.LogActivityAsync(new Core.Documents.ActivityLog
        {
            ProjectId = id,
            UserId = userId,
            UserName = userName,
            Action = "updated_project",
            EntityType = "project",
            EntityId = id,
            Metadata = new Dictionary<string, object>
            {
                { "projectName", project.Name }
            },
            Timestamp = DateTime.UtcNow
        });

        // Notify members via SignalR
        await _hubContext.Clients.Group($"project_{id}")
            .SendAsync("ProjectUpdated", new
            {
                ProjectId = id,
                Name = project.Name,
                Description = project.Description,
                UpdatedBy = userName,
                Timestamp = DateTime.UtcNow
            });

        var members = await _memberRepository.GetProjectMembersAsync(id);
        var taskCount = await _taskRepository.GetProjectTaskCountAsync(id);

        var dto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            OwnerId = project.OwnerId,
            OwnerName = project.Owner.FullName,
            CreatedAt = project.CreatedAt,
            MemberCount = members.Count(),
            TaskCount = taskCount
        };

        _logger.LogInformation("Project updated: {ProjectId} by user {UserId}", id, userId);

        return Ok(dto);
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound(new { error = "Project not found" });
        }

        var userId = GetCurrentUserId();
        var isOwner = await _projectRepository.IsUserOwnerAsync(id, userId);

        if (!isOwner)
        {
            return Forbid();
        }

        await _projectRepository.DeleteAsync(id);

        // Notify members via SignalR
        await _hubContext.Clients.Group($"project_{id}")
            .SendAsync("ProjectDeleted", new
            {
                ProjectId = id,
                DeletedBy = GetCurrentUserName(),
                Timestamp = DateTime.UtcNow
            });

        _logger.LogInformation("Project deleted: {ProjectId} by user {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Get project members
    /// </summary>
    [HttpGet("{id}/members")]
    [ProducesResponseType(typeof(IEnumerable<ProjectMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProjectMemberDto>>> GetProjectMembers(Guid id)
    {
        var userId = GetCurrentUserId();
        var isMember = await _projectRepository.IsUserMemberAsync(id, userId);

        if (!isMember)
        {
            return Forbid();
        }

        var members = await _memberRepository.GetProjectMembersAsync(id);

        var memberDtos = members.Select(m => new ProjectMemberDto
        {
            Id = m.Id,
            ProjectId = m.ProjectId,
            UserId = m.UserId,
            UserName = m.User.FullName,
            UserEmail = m.User.Email,
            Role = m.Role,
            JoinedAt = m.JoinedAt
        });

        return Ok(memberDtos);
    }

    /// <summary>
    /// Add a member to the project
    /// </summary>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if project exists
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound(new { error = "Project not found" });
        }

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        // Check if user is owner or admin
        var isOwner = await _projectRepository.IsUserOwnerAsync(id, userId);
        var userRole = await _memberRepository.GetUserRoleAsync(id, userId);

        if (!isOwner && userRole != ProjectRole.Admin)
        {
            return Forbid();
        }

        // Find user by email
        var newUser = await _userRepository.GetByEmailAsync(request.Email);
        if (newUser == null)
        {
            return NotFound(new { error = "User not found with that email" });
        }

        // Check if user is already a member
        var existingMember = await _memberRepository.GetByProjectAndUserAsync(id, newUser.Id);
        if (existingMember != null)
        {
            return BadRequest(new { error = "User is already a member of this project" });
        }

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = id,
            UserId = newUser.Id,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow
        };

        await _memberRepository.AddMemberAsync(member);

        // Log activity
        await _activityLogRepository.LogActivityAsync(new Core.Documents.ActivityLog
        {
            ProjectId = id,
            UserId = userId,
            UserName = userName,
            Action = "added_member",
            EntityType = "project_member",
            EntityId = member.Id,
            Metadata = new Dictionary<string, object>
            {
                { "newMemberName", newUser.FullName },
                { "newMemberEmail", newUser.Email },
                { "role", request.Role.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        // Notify via SignalR
        await _hubContext.Clients.Group($"project_{id}")
            .SendAsync("MemberAdded", new
            {
                ProjectId = id,
                Member = new
                {
                    Id = member.Id,
                    UserId = newUser.Id,
                    UserName = newUser.FullName,
                    UserEmail = newUser.Email,
                    Role = request.Role,
                    JoinedAt = member.JoinedAt
                },
                AddedBy = userName,
                Timestamp = DateTime.UtcNow
            });

        // Reload member with user details
        member = await _memberRepository.GetByIdAsync(member.Id);

        var dto = new ProjectMemberDto
        {
            Id = member!.Id,
            ProjectId = member.ProjectId,
            UserId = member.UserId,
            UserName = member.User.FullName,
            UserEmail = member.User.Email,
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };

        _logger.LogInformation("Member added to project {ProjectId}: {UserEmail} by {UserId}",
            id, request.Email, userId);

        return CreatedAtAction(nameof(GetProjectMembers), new { id }, dto);
    }

    /// <summary>
    /// Update member role
    /// </summary>
    [HttpPut("{projectId}/members/{memberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid projectId,
        Guid memberId,
        [FromBody] UpdateMemberRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var member = await _memberRepository.GetByIdAsync(memberId);
        if (member == null || member.ProjectId != projectId)
        {
            return NotFound(new { error = "Member not found" });
        }

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        // Check if user is owner or admin
        var isOwner = await _projectRepository.IsUserOwnerAsync(projectId, userId);
        var userRole = await _memberRepository.GetUserRoleAsync(projectId, userId);

        if (!isOwner && userRole != ProjectRole.Admin)
        {
            return Forbid();
        }

        // Don't allow changing owner's role
        if (member.UserId == (await _projectRepository.GetByIdAsync(projectId))!.OwnerId)
        {
            return BadRequest(new { error = "Cannot change project owner's role" });
        }

        var oldRole = member.Role;
        await _memberRepository.UpdateMemberRoleAsync(memberId, request.Role);

        // Log activity
        await _activityLogRepository.LogActivityAsync(new Core.Documents.ActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            UserName = userName,
            Action = "updated_member_role",
            EntityType = "project_member",
            EntityId = memberId,
            Metadata = new Dictionary<string, object>
            {
                { "memberName", member.User.FullName },
                { "oldRole", oldRole.ToString() },
                { "newRole", request.Role.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        // Notify via SignalR
        await _hubContext.Clients.Group($"project_{projectId}")
            .SendAsync("MemberRoleUpdated", new
            {
                ProjectId = projectId,
                MemberId = memberId,
                UserId = member.UserId,
                OldRole = oldRole.ToString(),
                NewRole = request.Role.ToString(),
                UpdatedBy = userName,
                Timestamp = DateTime.UtcNow
            });

        _logger.LogInformation("Member role updated in project {ProjectId}: {MemberId} by {UserId}",
            projectId, memberId, userId);

        return NoContent();
    }

    /// <summary>
    /// Remove a member from the project
    /// </summary>
    [HttpDelete("{projectId}/members/{memberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid memberId)
    {
        var member = await _memberRepository.GetByIdAsync(memberId);
        if (member == null || member.ProjectId != projectId)
        {
            return NotFound(new { error = "Member not found" });
        }

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        // Check if user is owner or admin, or removing themselves
        var isOwner = await _projectRepository.IsUserOwnerAsync(projectId, userId);
        var userRole = await _memberRepository.GetUserRoleAsync(projectId, userId);
        var isSelf = member.UserId == userId;

        if (!isOwner && userRole != ProjectRole.Admin && !isSelf)
        {
            return Forbid();
        }

        // Don't allow removing the owner
        if (member.UserId == (await _projectRepository.GetByIdAsync(projectId))!.OwnerId)
        {
            return BadRequest(new { error = "Cannot remove project owner" });
        }

        var removedUserName = member.User.FullName;

        await _memberRepository.RemoveMemberAsync(memberId);

        // Log activity
        await _activityLogRepository.LogActivityAsync(new Core.Documents.ActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            UserName = userName,
            Action = isSelf ? "left_project" : "removed_member",
            EntityType = "project_member",
            EntityId = memberId,
            Metadata = new Dictionary<string, object>
            {
                { "removedMemberName", removedUserName }
            },
            Timestamp = DateTime.UtcNow
        });

        // Notify via SignalR
        await _hubContext.Clients.Group($"project_{projectId}")
            .SendAsync("MemberRemoved", new
            {
                ProjectId = projectId,
                MemberId = memberId,
                UserId = member.UserId,
                UserName = removedUserName,
                RemovedBy = userName,
                IsSelfRemoval = isSelf,
                Timestamp = DateTime.UtcNow
            });

        _logger.LogInformation("Member removed from project {ProjectId}: {MemberId} by {UserId}",
            projectId, memberId, userId);

        return NoContent();
    }
}