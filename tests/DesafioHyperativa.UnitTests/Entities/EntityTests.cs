using DesafioHyperativa.Domain.Entities;
using FluentAssertions;

namespace DesafioHyperativa.UnitTests.Entities;

public class CardEntityTests
{
    [Fact]
    public void Card_Constructor_SetsAllProperties()
    {
        var before = DateTime.UtcNow;
        var card = new Card("hashABC", "encXYZ", "user42");
        var after = DateTime.UtcNow;

        card.Id.Should().NotBe(Guid.Empty);
        card.CardHash.Should().Be("hashABC");
        card.EncryptedCard.Should().Be("encXYZ");
        card.CreatedBy.Should().Be("user42");
        card.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Card_TwoInstances_HaveDifferentIds()
    {
        var c1 = new Card("hash1", "enc1", "u");
        var c2 = new Card("hash2", "enc2", "u");
        c1.Id.Should().NotBe(c2.Id);
    }

    [Fact]
    public void Card_CreatedAt_IsUtc()
    {
        var card = new Card("h", "e", "u");
        card.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Card_Properties_AreReadOnly()
    {
        // Verifica que os setters são privados (garantia de encapsulamento)
        var type = typeof(Card);
        var props = new[] { "Id", "CardHash", "EncryptedCard", "CreatedAt", "CreatedBy" };
        foreach (var name in props)
        {
            var prop = type.GetProperty(name)!;
            prop.SetMethod.Should().NotBeNull(because: $"{name} deve ter setter privado");
            prop.SetMethod!.IsPublic.Should().BeFalse(because: $"{name} setter deve ser privado");
        }
    }
}

public class UserEntityTests
{
    [Fact]
    public void User_Constructor_SetsAllProperties()
    {
        var before = DateTime.UtcNow;
        var user = new User("alice", "alice@example.com", "hash_password", ["Admin", "User"]);
        var after = DateTime.UtcNow;

        user.Id.Should().NotBe(Guid.Empty);
        user.Username.Should().Be("alice");
        user.Email.Should().Be("alice@example.com");
        user.PasswordHash.Should().Be("hash_password");
        user.Roles.Should().BeEquivalentTo(["Admin", "User"]);
        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void User_TwoInstances_HaveDifferentIds()
    {
        var u1 = new User("a", "a@x.com", "h", ["User"]);
        var u2 = new User("b", "b@x.com", "h", ["User"]);
        u1.Id.Should().NotBe(u2.Id);
    }

    [Fact]
    public void User_CreatedAt_IsUtc()
    {
        var user = new User("u", "u@x.com", "h", []);
        user.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void User_EmptyRoles_IsAllowed()
    {
        var user = new User("u", "u@x.com", "h", []);
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void User_SingleRole_IsStored()
    {
        var user = new User("u", "u@x.com", "h", ["Admin"]);
        user.Roles.Should().ContainSingle().Which.Should().Be("Admin");
    }
}
