using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.DTOs.Responses;
using DesafioHyperativa.Application.Interfaces;
using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Domain.Interfaces;
using DesafioHyperativa.Shared.Constants;
using DesafioHyperativa.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DesafioHyperativa.Application.Services;

public class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CardService> _logger;

    public CardService(
        ICardRepository cardRepository,
        IEncryptionService encryptionService,
        ILogger<CardService> logger)
    {
        _cardRepository = cardRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<CardResponse> AddCardAsync(AddCardRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        var normalized = CardHashHelper.Normalize(request.CardNumber);
        var hash = CardHashHelper.ComputeHash(normalized);

        // Idempotência: retorna o cartão existente se já foi cadastrado
        var existing = await _cardRepository.GetByHashAsync(hash, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Cartão já existente. Id: {CardId}", existing.Id);
            return new CardResponse(existing.Id);
        }

        var encrypted = _encryptionService.Encrypt(normalized);
        var card = new Card(hash, encrypted, createdBy);
        var saved = await _cardRepository.AddAsync(card, cancellationToken);

        _logger.LogInformation("Cartão inserido com sucesso. Id: {CardId}", saved.Id);
        return new CardResponse(saved.Id);
    }

    public async Task<UploadResponse> UploadCardsAsync(IFormFile file, string createdBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando upload de arquivo TXT. Arquivo: {FileName}, Tamanho: {Size} bytes",
            file.FileName, file.Length);

        var lines = new List<string>();
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                lines.Add(line.Trim());
        }

        var totalLines = lines.Count;
        var invalidCount = 0;
        var cardsToInsert = new List<Card>();
        var duplicateHashes = new HashSet<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                invalidCount++;
                continue;
            }

            var normalized = CardHashHelper.Normalize(line);
            var length = normalized.Length;

            if (length < AppConstants.Card.MinLength || length > AppConstants.Card.MaxLength ||
                !normalized.All(char.IsDigit))
            {
                _logger.LogDebug("Linha inválida ignorada: {Line}", line);
                invalidCount++;
                continue;
            }

            var hash = CardHashHelper.ComputeHash(normalized);

            // Evita duplicatas no mesmo arquivo
            if (duplicateHashes.Contains(hash))
            {
                invalidCount++;
                continue;
            }

            // Verifica se já existe no banco
            if (await _cardRepository.ExistsByHashAsync(hash, cancellationToken))
            {
                _logger.LogDebug("Cartão duplicado ignorado no upload.");
                invalidCount++;
                continue;
            }

            duplicateHashes.Add(hash);
            var encrypted = _encryptionService.Encrypt(normalized);
            cardsToInsert.Add(new Card(hash, encrypted, createdBy));
        }

        IReadOnlyList<Card> savedCards = [];
        if (cardsToInsert.Count > 0)
            savedCards = await _cardRepository.AddRangeAsync(cardsToInsert, cancellationToken);

        var processed = savedCards.Count;
        _logger.LogInformation("Upload concluído. Total: {Total}, Processados: {Processed}, Inválidos: {Invalid}",
            totalLines, processed, invalidCount + (cardsToInsert.Count - processed));

        return new UploadResponse(
            TotalLines: totalLines,
            Processed: processed,
            Invalid: totalLines - processed,
            GeneratedIds: savedCards.Select(c => c.Id).ToList()
        );
    }

    public async Task<CardExistsResponse> CheckCardExistsAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        var normalized = CardHashHelper.Normalize(cardNumber);
        var hash = CardHashHelper.ComputeHash(normalized);

        var card = await _cardRepository.GetByHashAsync(hash, cancellationToken);

        if (card is null)
            return new CardExistsResponse(false, null);

        return new CardExistsResponse(true, card.Id);
    }
}
