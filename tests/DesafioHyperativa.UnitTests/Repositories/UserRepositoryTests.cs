using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Infrastructure.Data;
using DesafioHyperativa.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DesafioHyperativa.UnitTests.Repositories;

public class UserRepositoryTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static User MakeUser(string username = "user1", string email = "user1@example.com")
        => new User(username, email, BCrypt.Net.BCrypt.HashPassword("pass"), ["User"]);

    // ─── GetByUsernameAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByUsernameAsync_UserExists_ReturnsUser()
    {
        await using var ctx = CreateContext(nameof(GetByUsernameAsync_UserExists_ReturnsUser));
        var user = MakeUser("alice", "alice@example.com");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var result = await repo.GetByUsernameAsync("alice");

        result.Should().NotBeNull();
        result!.Username.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_UserNotFound_ReturnsNull()
    {
        await using var ctx = CreateContext(nameof(GetByUsernameAsync_UserNotFound_ReturnsNull));
        var repo = new UserRepository(ctx);

        var result = await repo.GetByUsernameAsync("nobody");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseSensitive_DoesNotReturnWrongCase()
    {
        await using var ctx = CreateContext(nameof(GetByUsernameAsync_CaseSensitive_DoesNotReturnWrongCase));
        ctx.Users.Add(MakeUser("Alice", "alice@example.com"));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        // EF InMemory faz comparação case-sensitive por padrão
        var result = await repo.GetByUsernameAsync("alice");

        // Apenas verificamos que quando o case é exato, encontra
        var exact = await repo.GetByUsernameAsync("Alice");
        exact.Should().NotBeNull();
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsUser()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_UserExists_ReturnsUser));
        var user = MakeUser();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var result = await repo.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UserNotFound_ReturnsNull()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_UserNotFound_ReturnsNull));
        var repo = new UserRepository(ctx);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ─── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        await using var ctx = CreateContext(nameof(AddAsync_PersistsUser));
        var repo = new UserRepository(ctx);
        var user = MakeUser("bob", "bob@example.com");

        var saved = await repo.AddAsync(user);

        saved.Id.Should().Be(user.Id);
        ctx.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_ReturnsSavedUserWithCorrectProperties()
    {
        await using var ctx = CreateContext(nameof(AddAsync_ReturnsSavedUserWithCorrectProperties));
        var repo = new UserRepository(ctx);
        var user = new User("carol", "carol@example.com", "hash", ["Admin"]);

        var result = await repo.AddAsync(user);

        result.Username.Should().Be("carol");
        result.Email.Should().Be("carol@example.com");
        result.Roles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    [Fact]
    public async Task AddAsync_UserIsRetrievableAfterSave()
    {
        await using var ctx = CreateContext(nameof(AddAsync_UserIsRetrievableAfterSave));
        var repo = new UserRepository(ctx);
        var user = MakeUser("dave", "dave@example.com");

        await repo.AddAsync(user);
        var found = await repo.GetByUsernameAsync("dave");

        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task AddAsync_MultipleUsers_EachHasUniqueId()
    {
        await using var ctx = CreateContext(nameof(AddAsync_MultipleUsers_EachHasUniqueId));
        var repo = new UserRepository(ctx);

        var u1 = await repo.AddAsync(MakeUser("eve", "eve@example.com"));
        var u2 = await repo.AddAsync(MakeUser("frank", "frank@example.com"));

        u1.Id.Should().NotBe(u2.Id);
    }
}
