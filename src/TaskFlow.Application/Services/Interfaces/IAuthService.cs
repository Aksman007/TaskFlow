using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    Guid GetUserIdFromToken(string token);
}