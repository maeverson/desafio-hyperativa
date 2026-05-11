using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.Services;
using DesafioHyperativa.Application.Interfaces;
using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesafioHyperativa.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepoMock.Object, _jwtServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");
        var user = new User("testuser", "test@example.com", passwordHash, ["User"]);

        _userRepoMock
            .Setup(r => r.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns("fake_jwt_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("fake_refresh_token");

        // Act
        var result = await _sut.LoginAsync(new LoginRequest("testuser", "Password@123"));

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("fake_jwt_token");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(3600);
        result.RefreshToken.Should().Be("fake_refresh_token");
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorized()
    {
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("nonexistent", "Password@123"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Usuário ou senha inválidos.");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        var user = new User("testuser", "test@example.com", passwordHash, ["User"]);

        _userRepoMock
            .Setup(r => r.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginRequest("testuser", "WrongPassword"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Usuário ou senha inválidos.");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_CallsGenerateTokenOnce()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");
        var user = new User("testuser", "test@example.com", passwordHash, ["Admin"]);

        _userRepoMock
            .Setup(r => r.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns("tok");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("ref");

        await _sut.LoginAsync(new LoginRequest("testuser", "Password@123"));

        _jwtServiceMock.Verify(j => j.GenerateToken(user), Times.Once);
        _jwtServiceMock.Verify(j => j.GenerateRefreshToken(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_NeverCallsGenerateToken()
    {
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        try { await _sut.LoginAsync(new LoginRequest("ghost", "pass")); } catch { }

        _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithCancellationToken_PassesTokenToRepository()
    {
        var cts = new CancellationTokenSource();
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), cts.Token))
            .ReturnsAsync((User?)null);

        try { await _sut.LoginAsync(new LoginRequest("u", "p"), cts.Token); } catch { }

        _userRepoMock.Verify(r => r.GetByUsernameAsync("u", cts.Token), Times.Once);
    }
}
