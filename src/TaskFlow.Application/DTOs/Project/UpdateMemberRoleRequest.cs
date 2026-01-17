using System.ComponentModel.DataAnnotations;
using TaskFlow.Core.Enums;

namespace TaskFlow.Application.DTOs.Project;

public class UpdateMemberRoleRequest
{
    [Required]
    public ProjectRole Role { get; set; }
}