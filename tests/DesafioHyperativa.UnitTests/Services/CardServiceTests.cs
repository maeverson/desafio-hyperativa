using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.Interfaces;
using DesafioHyperativa.Application.Services;
using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Domain.Interfaces;
using DesafioHyperativa.Shared.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace DesafioHyperativa.UnitTests.Services;

public class CardServiceTests
{
    private readonly Mock<ICardRepository> _cardRepoMock = new();
    private readonly Mock<IEncryptionService> _encryptionMock = new();
    private readonly Mock<ILogger<CardService>> _loggerMock = new();
    private readonly CardService _sut;

    public CardServiceTests()
    {
        _encryptionMock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted_value");
        _sut = new CardService(_cardRepoMock.Object, _encryptionMock.Object, _loggerMock.Object);
    }

    // ─── AddCardAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCardAsync_NewCard_ReturnsNewId()
    {
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card?)null);
        _cardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card c, CancellationToken _) => c);

        var result = await _sut.AddCardAsync(new AddCardRequest("4111111111111111"), "user1");

        result.CardId.Should().NotBe(Guid.Empty);
        _cardRepoMock.Verify(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCardAsync_DuplicateCard_ReturnsExistingIdWithoutInserting()
    {
        var existingCard = new Card("hash123", "encrypted", "user1");
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCard);

        var result = await _sut.AddCardAsync(new AddCardRequest("4111111111111111"), "user1");

        result.CardId.Should().Be(existingCard.Id);
        _cardRepoMock.Verify(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCardAsync_NormalizesCardBeforeHashing()
    {
        // Cartão com espaços deve ter o mesmo hash que sem espaços
        string? capturedHash = null;
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((h, _) => capturedHash = h)
            .ReturnsAsync((Card?)null);
        _cardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card c, CancellationToken _) => c);

        await _sut.AddCardAsync(new AddCardRequest("4111 1111 1111 1111"), "u");

        capturedHash.Should().Be(CardHashHelper.ComputeHash("4111111111111111"));
    }

    [Fact]
    public async Task AddCardAsync_EncryptsNormalizedCard()
    {
        string? capturedPlain = null;
        _encryptionMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Callback<string>(p => capturedPlain = p)
            .Returns("enc");
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card?)null);
        _cardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card c, CancellationToken _) => c);

        await _sut.AddCardAsync(new AddCardRequest("4111-1111-1111-1111"), "u");

        capturedPlain.Should().Be("4111111111111111");
    }

    [Fact]
    public async Task AddCardAsync_StoresCreatedByCorrectly()
    {
        Card? savedCard = null;
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card?)null);
        _cardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .Callback<Card, CancellationToken>((c, _) => savedCard = c)
            .ReturnsAsync((Card c, CancellationToken _) => c);

        await _sut.AddCardAsync(new AddCardRequest("4111111111111111"), "operador42");

        savedCard!.CreatedBy.Should().Be("operador42");
    }

    // ─── CheckCardExistsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckCardExistsAsync_ExistingCard_ReturnsExistsTrue()
    {
        var existingCard = new Card("hash", "enc", "u");
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCard);

        var result = await _sut.CheckCardExistsAsync("4111111111111111");

        result.Exists.Should().BeTrue();
        result.CardId.Should().Be(existingCard.Id);
    }

    [Fact]
    public async Task CheckCardExistsAsync_NonExistingCard_ReturnsExistsFalse()
    {
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card?)null);

        var result = await _sut.CheckCardExistsAsync("4111111111111111");

        result.Exists.Should().BeFalse();
        result.CardId.Should().BeNull();
    }

    [Fact]
    public async Task CheckCardExistsAsync_UsesHashOfNormalizedNumber()
    {
        string? capturedHash = null;
        _cardRepoMock
            .Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((h, _) => capturedHash = h)
            .ReturnsAsync((Card?)null);

        await _sut.CheckCardExistsAsync("4111 1111 1111 1111");

        capturedHash.Should().Be(CardHashHelper.ComputeHash("4111111111111111"));
    }

    // ─── UploadCardsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadCardsAsync_ValidFile_ProcessesOnlyValidAndUniqueCards()
    {
        // 5 linhas: 2 válidas únicas, 1 linha em branco, 1 inválida, 1 duplicata
        var content = "4111111111111111\n5500005555555559\n\nINVALID\n4111111111111111";
        _cardRepoMock
            .Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cardRepoMock
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Card>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Card> cards, CancellationToken _) => cards.ToList());

        var result = await _sut.UploadCardsAsync(BuildFile(content), "u");

        result.TotalLines.Should().Be(5);
        result.Processed.Should().Be(2);
        result.Invalid.Should().Be(3);
        result.GeneratedIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadCardsAsync_AllInvalidLines_ReturnsZeroProcessed()
    {
        var content = "INVALID\nabc\n123\n";
        _cardRepoMock
            .Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cardRepoMock
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Card>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Card> _, CancellationToken _) => new List<Card>());

        var result = await _sut.UploadCardsAsync(BuildFile(content), "u");

        result.Processed.Should().Be(0);
        _cardRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Card>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadCardsAsync_CardAlreadyExistsInDb_CountsAsInvalid()
    {
        var content = "4111111111111111\n5500005555555559";
        // Primeiro cartão já existe, segundo é novo
        _cardRepoMock
            .Setup(r => r.ExistsByHashAsync(CardHashHelper.ComputeHash("4111111111111111"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cardRepoMock
            .Setup(r => r.ExistsByHashAsync(CardHashHelper.ComputeHash("5500005555555559"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cardRepoMock
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Card>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Card> cards, CancellationToken _) => cards.ToList());

        var result = await _sut.UploadCardsAsync(BuildFile(content), "u");

        result.TotalLines.Should().Be(2);
        result.Processed.Should().Be(1);
        result.Invalid.Should().Be(1);
    }

    [Fact]
    public async Task UploadCardsAsync_GeneratedIds_MatchSavedCards()
    {
        var content = "4111111111111111\n5500005555555559";
        _cardRepoMock
            .Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cardRepoMock
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Card>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Card> cards, CancellationToken _) => cards.ToList());

        var result = await _sut.UploadCardsAsync(BuildFile(content), "u");

        result.GeneratedIds.Should().HaveCount(2);
        result.GeneratedIds.Should().AllSatisfy(id => id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task UploadCardsAsync_EmptyFile_ReturnsZeroEverything()
    {
        _cardRepoMock
            .Setup(r => r.ExistsByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cardRepoMock
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Card>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Card>());

        var result = await _sut.UploadCardsAsync(BuildFile(""), "u");

        result.Processed.Should().Be(0);
        result.GeneratedIds.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static IFormFile BuildFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        file.Setup(f => f.Length).Returns(bytes.Length);
        file.Setup(f => f.FileName).Returns("cards.txt");
        return file.Object;
    }
}
