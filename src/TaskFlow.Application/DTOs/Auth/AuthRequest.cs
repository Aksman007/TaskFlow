using TaskFlow.Application.DTOs.User;

namespace TaskFlow.Application.DTOs.Auth;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
    public string? Error { get; set; }
    public DateTime? ExpiresAt { get; set; }
}