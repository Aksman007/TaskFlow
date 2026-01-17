using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        ITaskRepository taskRepository,
        ILogger<ProjectsController> logger)
    {
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _taskRepository = taskRepository;
        _logger = logger;
    }

    [HttpGet]
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

    [HttpGet("{id}")]
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

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var userId = GetCurrentUserId();

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
}

public class CreateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}