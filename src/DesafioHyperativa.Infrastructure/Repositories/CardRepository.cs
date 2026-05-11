using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Domain.Interfaces;
using DesafioHyperativa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DesafioHyperativa.Infrastructure.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _context;

    public CardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Card?> GetByHashAsync(string cardHash, CancellationToken cancellationToken = default)
        => await _context.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CardHash == cardHash, cancellationToken);

    public async Task<Card> AddAsync(Card card, CancellationToken cancellationToken = default)
    {
        await _context.Cards.AddAsync(card, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return card;
    }

    public async Task<IReadOnlyList<Card>> AddRangeAsync(IEnumerable<Card> cards, CancellationToken cancellationToken = default)
    {
        var list = cards.ToList();
        await _context.Cards.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return list;
    }

    public async Task<bool> ExistsByHashAsync(string cardHash, CancellationToken cancellationToken = default)
        => await _context.Cards.AnyAsync(c => c.CardHash == cardHash, cancellationToken);
}
