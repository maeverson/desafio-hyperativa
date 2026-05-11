namespace DesafioHyperativa.Domain.Entities;

public class Card
{
    public Guid Id { get; private set; }
    public string CardHash { get; private set; } = null!;
    public string EncryptedCard { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;

    protected Card() { }

    public Card(string cardHash, string encryptedCard, string createdBy)
    {
        Id = Guid.NewGuid();
        CardHash = cardHash;
        EncryptedCard = encryptedCard;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }
}
