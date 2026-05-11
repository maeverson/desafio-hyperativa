using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DesafioHyperativa.API.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
[Produces("application/json")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    private readonly IValidator<AddCardRequest> _validator;

    public CardsController(ICardService cardService, IValidator<AddCardRequest> validator)
    {
        _cardService = cardService;
        _validator = validator;
    }

    /// <summary>
    /// Insere um novo número de cartão de forma unitária.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddCard([FromBody] AddCardRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var createdBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var result = await _cardService.AddCardAsync(request, createdBy, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Faz upload de arquivo TXT com um número de cartão por linha.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadCards(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Arquivo inválido ou vazio." });

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Apenas arquivos .txt são permitidos." });

        var createdBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var result = await _cardService.UploadCardsAsync(file, createdBy, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Consulta se um número de cartão existe na base.
    /// </summary>
    /// <param name="cardNumber">Número do cartão a consultar.</param>
    [HttpGet("{cardNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckCard([FromRoute] string cardNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return BadRequest(new { message = "Número do cartão é obrigatório." });

        var result = await _cardService.CheckCardExistsAsync(cardNumber, cancellationToken);
        return Ok(result);
    }
}
