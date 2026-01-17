using System.ComponentModel.DataAnnotations;
using TaskFlow.Core.Enums;

namespace TaskFlow.Application.DTOs.Project;

public class AddMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public ProjectRole Role { get; set; } = ProjectRole.Member;
}