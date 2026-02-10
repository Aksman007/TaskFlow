using FluentValidation;
using TaskFlow.Application.DTOs.Project;

namespace TaskFlow.Application.Validators;

public class UpdateMemberRoleRequestValidator : AbstractValidator<UpdateMemberRoleRequest>
{
    public UpdateMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role value");
    }
}
