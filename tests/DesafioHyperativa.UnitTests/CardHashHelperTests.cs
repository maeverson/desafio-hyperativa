using DesafioHyperativa.Shared.Helpers;
using FluentAssertions;

namespace DesafioHyperativa.UnitTests;

public class CardHashHelperTests
{
    // ─── ComputeHash ──────────────────────────────────────────────────────────

    [Fact]
    public void ComputeHash_SameInput_ReturnsSameHash()
    {
        var h1 = CardHashHelper.ComputeHash("4111111111111111");
        var h2 = CardHashHelper.ComputeHash("4111111111111111");
        h1.Should().Be(h2);
    }

    [Fact]
    public void ComputeHash_DifferentInputs_ReturnDifferentHashes()
    {
        var h1 = CardHashHelper.ComputeHash("4111111111111111");
        var h2 = CardHashHelper.ComputeHash("5500005555555559");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void ComputeHash_Returns64CharLowercaseHex()
    {
        var hash = CardHashHelper.ComputeHash("4111111111111111");
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void ComputeHash_NormalizesBeforeHashing_SpacesAndDashes()
    {
        var h1 = CardHashHelper.ComputeHash("4111111111111111");
        var h2 = CardHashHelper.ComputeHash("4111 1111 1111 1111");
        var h3 = CardHashHelper.ComputeHash("4111-1111-1111-1111");
        h1.Should().Be(h2).And.Be(h3);
    }

    [Fact]
    public void ComputeHash_KnownValue_ReturnsCorrectHash()
    {
        // SHA-256("4111111111111111") - valor verificado
        var hash = CardHashHelper.ComputeHash("4111111111111111");
        hash.Should().Be("9bbef19476623ca56c17da75fd57734dbf82530686043a6e491c6d71befe8f6e");
    }

    // ─── Normalize ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("4111111111111111", "4111111111111111")]
    [InlineData(" 4111 1111 1111 1111 ", "4111111111111111")]
    [InlineData("4111-1111-1111-1111", "4111111111111111")]
    [InlineData("  4111-1111 1111 1111  ", "4111111111111111")]
    public void Normalize_RemovesSpacesAndDashes(string input, string expected)
    {
        CardHashHelper.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_EmptyString_ReturnsEmpty()
    {
        CardHashHelper.Normalize("").Should().BeEmpty();
    }

    [Fact]
    public void Normalize_OnlySpaces_ReturnsEmpty()
    {
        CardHashHelper.Normalize("   ").Should().BeEmpty();
    }

    // ─── IsValidLuhn ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("4111111111111111", true)]    // Visa (padrão de teste)
    [InlineData("5500005555555559", true)]    // Mastercard
    [InlineData("378282246310005", true)]     // Amex
    [InlineData("6011111111111117", true)]    // Discover
    [InlineData("4012888888881881", true)]    // Visa
    [InlineData("5555555555554444", true)]    // Mastercard
    public void IsValidLuhn_ValidCards_ReturnTrue(string cardNumber, bool expected)
    {
        CardHashHelper.IsValidLuhn(cardNumber).Should().Be(expected);
    }

    [Theory]
    [InlineData("1234567890123456", false)]  // Falha Luhn
    [InlineData("4111111111111112", false)]  // Último dígito errado
    [InlineData("5500005555555550", false)]  // Mastercard dígito errado
    public void IsValidLuhn_InvalidCards_ReturnFalse(string cardNumber, bool expected)
    {
        CardHashHelper.IsValidLuhn(cardNumber).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCD1234EFGH5678")]
    public void IsValidLuhn_NonDigitOrEmpty_ReturnFalse(string cardNumber)
    {
        CardHashHelper.IsValidLuhn(cardNumber).Should().BeFalse();
    }
}
