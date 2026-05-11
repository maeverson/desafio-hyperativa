using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Infrastructure.Data;
using DesafioHyperativa.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DesafioHyperativa.UnitTests.Repositories;

/// <summary>
/// Testes de repositório usando banco de dados em memória (EF InMemory).
/// Cada teste cria seu próprio contexto isolado.
/// </summary>
public class CardRepositoryTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // ─── GetByHashAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByHashAsync_CardNotFound_ReturnsNull()
    {
        await using var ctx = CreateContext(nameof(GetByHashAsync_CardNotFound_ReturnsNull));
        var repo = new CardRepository(ctx);

        var result = await repo.GetByHashAsync("nonexistent_hash");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByHashAsync_CardExists_ReturnsCard()
    {
        await using var ctx = CreateContext(nameof(GetByHashAsync_CardExists_ReturnsCard));
        var card = new Card("hash_abc", "encrypted_abc", "user1");
        ctx.Cards.Add(card);
        await ctx.SaveChangesAsync();

        var repo = new CardRepository(ctx);
        var result = await repo.GetByHashAsync("hash_abc");

        result.Should().NotBeNull();
        result!.Id.Should().Be(card.Id);
        result.CardHash.Should().Be("hash_abc");
        result.CreatedBy.Should().Be("user1");
    }

    [Fact]
    public async Task GetByHashAsync_WrongHash_ReturnsNull()
    {
        await using var ctx = CreateContext(nameof(GetByHashAsync_WrongHash_ReturnsNull));
        ctx.Cards.Add(new Card("hash_correct", "enc", "u"));
        await ctx.SaveChangesAsync();

        var repo = new CardRepository(ctx);
        var result = await repo.GetByHashAsync("hash_wrong");

        result.Should().BeNull();
    }

    // ─── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsCard()
    {
        await using var ctx = CreateContext(nameof(AddAsync_PersistsCard));
        var repo = new CardRepository(ctx);
        var card = new Card("hash1", "enc1", "user1");

        var saved = await repo.AddAsync(card);

        saved.Id.Should().Be(card.Id);
        ctx.Cards.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_ReturnsSavedCard()
    {
        await using var ctx = CreateContext(nameof(AddAsync_ReturnsSavedCard));
        var repo = new CardRepository(ctx);
        var card = new Card("h", "e", "u");

        var result = await repo.AddAsync(card);

        result.Should().NotBeNull();
        result.Id.Should().Be(card.Id);
    }

    [Fact]
    public async Task AddAsync_MultipleCalls_EachCardHasUniqueId()
    {
        await using var ctx = CreateContext(nameof(AddAsync_MultipleCalls_EachCardHasUniqueId));
        var repo = new CardRepository(ctx);

        var c1 = await repo.AddAsync(new Card("h1", "e1", "u"));
        var c2 = await repo.AddAsync(new Card("h2", "e2", "u"));

        c1.Id.Should().NotBe(c2.Id);
        ctx.Cards.Should().HaveCount(2);
    }

    // ─── AddRangeAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRangeAsync_PersistsAllCards()
    {
        await using var ctx = CreateContext(nameof(AddRangeAsync_PersistsAllCards));
        var repo = new CardRepository(ctx);
        var cards = Enumerable.Range(1, 5)
            .Select(i => new Card($"hash{i}", $"enc{i}", "u"))
            .ToList();

        var result = await repo.AddRangeAsync(cards);

        result.Should().HaveCount(5);
        ctx.Cards.Should().HaveCount(5);
    }

    [Fact]
    public async Task AddRangeAsync_EmptyList_PersistsNothing()
    {
        await using var ctx = CreateContext(nameof(AddRangeAsync_EmptyList_PersistsNothing));
        var repo = new CardRepository(ctx);

        var result = await repo.AddRangeAsync([]);

        result.Should().BeEmpty();
        ctx.Cards.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRangeAsync_ReturnsCorrectIds()
    {
        await using var ctx = CreateContext(nameof(AddRangeAsync_ReturnsCorrectIds));
        var repo = new CardRepository(ctx);
        var cards = new List<Card>
        {
            new Card("h1", "e1", "u"),
            new Card("h2", "e2", "u")
        };

        var result = await repo.AddRangeAsync(cards);

        result.Select(c => c.Id).Should().BeEquivalentTo(cards.Select(c => c.Id));
    }

    // ─── ExistsByHashAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsByHashAsync_CardExists_ReturnsTrue()
    {
        await using var ctx = CreateContext(nameof(ExistsByHashAsync_CardExists_ReturnsTrue));
        ctx.Cards.Add(new Card("hash_exists", "enc", "u"));
        await ctx.SaveChangesAsync();

        var repo = new CardRepository(ctx);
        var result = await repo.ExistsByHashAsync("hash_exists");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByHashAsync_CardNotFound_ReturnsFalse()
    {
        await using var ctx = CreateContext(nameof(ExistsByHashAsync_CardNotFound_ReturnsFalse));
        var repo = new CardRepository(ctx);

        var result = await repo.ExistsByHashAsync("not_there");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByHashAsync_EmptyDb_ReturnsFalse()
    {
        await using var ctx = CreateContext(nameof(ExistsByHashAsync_EmptyDb_ReturnsFalse));
        var repo = new CardRepository(ctx);

        var result = await repo.ExistsByHashAsync("any_hash");

        result.Should().BeFalse();
    }
}
