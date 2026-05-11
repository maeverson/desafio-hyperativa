using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace DesafioHyperativa.Application.Interfaces;

public interface ICardService
{
    Task<CardResponse> AddCardAsync(AddCardRequest request, string createdBy, CancellationToken cancellationToken = default);
    Task<UploadResponse> UploadCardsAsync(IFormFile file, string createdBy, CancellationToken cancellationToken = default);
    Task<CardExistsResponse> CheckCardExistsAsync(string cardNumber, CancellationToken cancellationToken = default);
}
