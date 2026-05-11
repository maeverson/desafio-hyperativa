using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Shared.Constants;
using DesafioHyperativa.Shared.Helpers;
using FluentValidation;

namespace DesafioHyperativa.Application.Validators;

public class AddCardRequestValidator : AbstractValidator<AddCardRequest>
{
    public AddCardRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("O número do cartão é obrigatório.")
            .Must(BeValidCardNumber)
            .WithMessage($"Número de cartão inválido. Deve conter entre {AppConstants.Card.MinLength} e {AppConstants.Card.MaxLength} dígitos.");
    }

    private static bool BeValidCardNumber(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return false;
        var normalized = CardHashHelper.Normalize(cardNumber);
        return normalized.Length >= AppConstants.Card.MinLength
            && normalized.Length <= AppConstants.Card.MaxLength
            && normalized.All(char.IsDigit);
    }
}
