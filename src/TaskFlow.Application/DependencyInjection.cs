using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Services;
using TaskFlow.Application.Services.Interfaces;
using TaskFlow.Application.Validators;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IAuthService, AuthService>();

        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        return services;
    }
}
