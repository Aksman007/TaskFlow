using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Services;
using TaskFlow.Application.Services.Interfaces;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IAuthService, AuthService>();

        // Add AutoMapper if needed
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}