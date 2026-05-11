using DesafioHyperativa.Domain.Entities;

namespace DesafioHyperativa.Domain.Interfaces;

public interface ICardRepository
{
    Task<Card?> GetByHashAsync(string cardHash, CancellationToken cancellationToken = default);
    Task<Card> AddAsync(Card card, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Card>> AddRangeAsync(IEnumerable<Card> cards, CancellationToken cancellationToken = default);
    Task<bool> ExistsByHashAsync(string cardHash, CancellationToken cancellationToken = default);
}
