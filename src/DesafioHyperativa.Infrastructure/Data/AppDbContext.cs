using DesafioHyperativa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesafioHyperativa.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("cards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.CardHash).HasColumnName("card_hash").IsRequired().HasMaxLength(64);
            entity.Property(e => e.EncryptedCard).HasColumnName("encrypted_card").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(256);

            // Índice no hash para buscas eficientes em milhões de registros
            entity.HasIndex(e => e.CardHash)
                  .IsUnique()
                  .HasDatabaseName("ix_cards_card_hash");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.Roles).HasColumnName("roles").HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<string[]>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToArray()
                ));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("ix_users_username");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_users_email");
        });
    }
}
