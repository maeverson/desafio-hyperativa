namespace DesafioHyperativa.Application.DTOs.Responses;

public record UploadResponse(
    int TotalLines,
    int Processed,
    int Invalid,
    IReadOnlyList<Guid> GeneratedIds
);
