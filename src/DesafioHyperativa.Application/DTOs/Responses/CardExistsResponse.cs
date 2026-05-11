namespace DesafioHyperativa.Application.DTOs.Responses;

public record CardExistsResponse(bool Exists, Guid? CardId);
