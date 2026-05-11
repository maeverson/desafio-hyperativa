using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.Validators;
using FluentAssertions;

namespace DesafioHyperativa.UnitTests.Validators;

public class AddCardRequestValidatorTests
{
    private readonly AddCardRequestValidator _validator = new();

    [Theory]
    [InlineData("4111111111111111")]        // Visa 16 dígitos
    [InlineData("5500005555555559")]        // Mastercard 16 dígitos
    [InlineData("378282246310005")]         // Amex 15 dígitos
    [InlineData("6011111111111117")]        // Discover 16 dígitos
    [InlineData("4111 1111 1111 1111")]     // Com espaços (normalizado)
    [InlineData("4111-1111-1111-1111")]     // Com traços (normalizado)
    [InlineData("4000000000000002")]        // Mínimo válido 16 dígitos
    [InlineData("5555555555554444")]        // Mastercard 16 dígitos
    public async Task Validate_ValidCardNumber_ShouldPass(string cardNumber)
    {
        var result = await _validator.ValidateAsync(new AddCardRequest(cardNumber));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]                           // Vazio
    [InlineData("  ")]                         // Apenas espaços
    [InlineData("123")]                        // Muito curto (3 dígitos)
    [InlineData("123456789012")]               // 12 dígitos (abaixo do mínimo)
    [InlineData("12345678901234567890")]        // 20 dígitos (acima do máximo)
    [InlineData("ABCD1234EFGH5678")]           // Letras
    [InlineData("4111-ABCD-1111-1111")]        // Letras misturadas
    [InlineData(null!)]                        // Nulo
    public async Task Validate_InvalidCardNumber_ShouldFail(string? cardNumber)
    {
        var result = await _validator.ValidateAsync(new AddCardRequest(cardNumber!));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_Invalid_ContainsErrorMessage()
    {
        var result = await _validator.ValidateAsync(new AddCardRequest("123"));
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].ErrorMessage.Should().Contain("cartão");
    }

    [Fact]
    public async Task Validate_Null_ContainsRequiredErrorMessage()
    {
        var result = await _validator.ValidateAsync(new AddCardRequest(null!));
        result.Errors.Should().NotBeEmpty();
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Theory]
    [InlineData("admin", "Admin@123")]
    [InlineData("user1", "Password1")]
    [InlineData("a", "123456")]               // Username mínimo 1 char, senha mínima 6
    public async Task Validate_ValidRequest_ShouldPass(string username, string password)
    {
        var result = await _validator.ValidateAsync(new LoginRequest(username, password));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Password@123")]          // Username vazio
    [InlineData("user", "")]                  // Senha vazia
    [InlineData("user", "12345")]             // Senha muito curta (5 chars)
    [InlineData(null!, "Password@123")]       // Username nulo
    [InlineData("user", null!)]              // Senha nula
    public async Task Validate_InvalidRequest_ShouldFail(string? username, string? password)
    {
        var result = await _validator.ValidateAsync(new LoginRequest(username!, password!));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_UsernameExceedsMaxLength_ShouldFail()
    {
        var longUsername = new string('a', 101);
        var result = await _validator.ValidateAsync(new LoginRequest(longUsername, "Password@123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_UsernameAtMaxLength_ShouldPass()
    {
        var maxUsername = new string('a', 100);
        var result = await _validator.ValidateAsync(new LoginRequest(maxUsername, "Password@123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyUsername_HasDescriptiveError()
    {
        var result = await _validator.ValidateAsync(new LoginRequest("", "Password@123"));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("usuário"));
    }

    [Fact]
    public async Task Validate_ShortPassword_HasDescriptiveError()
    {
        var result = await _validator.ValidateAsync(new LoginRequest("user", "12345"));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("senha"));
    }
}
