namespace DesafioHyperativa.Application.DTOs.Responses;

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken = null
);
