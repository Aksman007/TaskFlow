using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TaskFlow.Core.Interfaces.Repositories;

namespace TaskFlow.API.Attributes;

/// <summary>
/// Ensures user is a member of the project
/// </summary>
public class ProjectMemberAttribute : TypeFilterAttribute
{
    public ProjectMemberAttribute() : base(typeof(ProjectMemberFilter))
    {
    }
}

public class ProjectMemberFilter : IAsyncActionFilter
{
    private readonly IProjectRepository _projectRepository;

    public ProjectMemberFilter(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = Guid.Parse(userIdClaim.Value);

        // Try to get projectId from route parameters
        Guid projectId = Guid.Empty;
        if (context.ActionArguments.ContainsKey("projectId"))
        {
            projectId = (Guid)context.ActionArguments["projectId"];
        }
        else if (context.HttpContext.Request.RouteValues.ContainsKey("projectId"))
        {
            projectId = Guid.Parse(context.HttpContext.Request.RouteValues["projectId"]?.ToString() ?? string.Empty);
        }

        if (projectId == Guid.Empty)
        {
            context.Result = new BadRequestObjectResult(new { error = "Project ID is required" });
            return;
        }

        var isMember = await _projectRepository.IsUserMemberAsync(projectId, userId);

        if (!isMember)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}