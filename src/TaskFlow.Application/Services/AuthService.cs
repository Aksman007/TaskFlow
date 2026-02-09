using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.User;
using TaskFlow.Application.Services.Interfaces;
using TaskFlow.Core.Entities;
using TaskFlow.Core.Interfaces;
using TaskFlow.Core.Interfaces.Repositories;
using BCrypt.Net;

namespace TaskFlow.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;

        _jwtSecret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "TaskFlow";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "TaskFlowUsers";
        _accessTokenMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        _refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        // Validate JWT Secret length
        if (_jwtSecret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret must be at least 32 characters long");
        }
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResult
                {
                    Success = false,
                    Error = "A user with this email already exists"
                };
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            return new AuthResult
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Error = $"Registration failed: {ex.Message}"
            };
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Error = "Invalid email or password"
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResult
                {
                    Success = false,
                    Error = "Invalid email or password"
                };
            }

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            return new AuthResult
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Error = $"Login failed: {ex.Message}"
            };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshTokenValue)
    {
        try
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenValue);

            if (storedToken == null || !storedToken.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    Error = "Invalid or expired refresh token"
                };
            }

            var user = storedToken.User;

            // Revoke the old refresh token (rotation)
            await _refreshTokenRepository.RevokeAsync(refreshTokenValue);

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

            return new AuthResult
            {
                Success = true,
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Error = $"Token refresh failed: {ex.Message}"
            };
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshTokenValue)
    {
        await _refreshTokenRepository.RevokeAsync(refreshTokenValue);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = Guid.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);

            // Check if user still exists
            return await _userRepository.ExistsAsync(userId);
        }
        catch
        {
            return false;
        }
    }

    public Guid GetUserIdFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
        return Guid.Parse(userIdClaim);
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);
        return refreshToken;
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt
        };
    }
}
