using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.User;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IProjectMemberRepository memberRepository,
        ITaskRepository taskRepository,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _taskRepository = taskRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get users by email (for adding to projects)
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SearchUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get user's assigned tasks
    /// </summary>
    [HttpGet("me/tasks")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetMyTasks()
    {
        var userId = GetCurrentUserId();
        var tasks = await _taskRepository.GetUserTasksAsync(userId);

        var taskDtos = tasks.Select(t => new
        {
            t.Id,
            t.Title,
            t.Description,
            t.ProjectId,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            t.CreatedAt,
            t.DueDate
        });

        return Ok(taskDtos);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        user.FullName = request.FullName;

        await _userRepository.UpdateAsync(user);

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt
        };

        _logger.LogInformation("User profile updated: {UserId}", userId);

        return Ok(dto);
    }
}

public class UpdateUserRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}