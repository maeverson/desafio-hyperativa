using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.DTOs.Responses;
using DesafioHyperativa.Application.Interfaces;
using DesafioHyperativa.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DesafioHyperativa.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tentativa de login para usuário: {Username}", request.Username);

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Falha de login para usuário: {Username}", request.Username);
            throw new UnauthorizedAccessException("Usuário ou senha inválidos.");
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        _logger.LogInformation("Login bem-sucedido para usuário: {Username}", request.Username);

        return new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 3600,
            RefreshToken: refreshToken
        );
    }
}
