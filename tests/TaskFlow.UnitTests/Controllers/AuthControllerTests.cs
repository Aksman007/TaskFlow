using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.API.Controllers;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.User;
using TaskFlow.Application.Services.Interfaces;
using TaskFlow.UnitTests.Helpers;

namespace TaskFlow.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns("15");

        _controller = new AuthController(
            _authServiceMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);

        // Set up HttpContext so Response.Cookies works
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOkWithUserInfo()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FullName = "Test User"
        };

        var expectedResult = new AuthResult
        {
            Success = true,
            Token = "jwt-token",
            RefreshToken = "refresh-token",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow
            },
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new
        {
            success = true,
            user = expectedResult.User,
            expiresAt = expectedResult.ExpiresAt
        });
    }

    [Fact]
    public async Task Register_WhenUserExists_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FullName = "Test User"
        };

        var expectedResult = new AuthResult
        {
            Success = false,
            Error = "A user with this email already exists"
        };

        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { error = expectedResult.Error });
    }

    [Fact]
    public async Task Register_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest();
        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithUserInfo()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var expectedResult = new AuthResult
        {
            Success = true,
            Token = "jwt-token",
            RefreshToken = "refresh-token",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow
            },
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new
        {
            success = true,
            user = expectedResult.User,
            expiresAt = expectedResult.ExpiresAt
        });
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var expectedResult = new AuthResult
        {
            Success = false,
            Error = "Invalid email or password"
        };

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { error = expectedResult.Error });
    }

    [Fact]
    public async Task Login_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest();
        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public void ValidateToken_WithAuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        TestHelpers.SetupControllerContext(_controller, userId, "test@example.com", "Test User");

        // Act
        var result = _controller.ValidateToken();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public void GetCurrentUser_WithAuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var fullName = "Test User";
        TestHelpers.SetupControllerContext(_controller, userId, email, fullName);

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ReturnsOkWithMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        TestHelpers.SetupControllerContext(_controller, userId);

        _authServiceMock
            .Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Logged out successfully" });
    }

    #endregion
}
