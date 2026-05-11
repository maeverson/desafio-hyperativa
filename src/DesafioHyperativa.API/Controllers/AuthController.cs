using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DesafioHyperativa.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _validator;

    public AuthController(IAuthService authService, IValidator<LoginRequest> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    /// <summary>
    /// Realiza autenticação e retorna um JWT Bearer Token.
    /// </summary>
    /// <param name="request">Credenciais do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
}
