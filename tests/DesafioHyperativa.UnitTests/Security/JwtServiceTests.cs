using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DesafioHyperativa.UnitTests.Security;

public class JwtServiceTests
{
    private static JwtService BuildService(int expiresInMinutes = 60)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]        = "TestSecretKey!2024_Must_Be_At_Least_32_Chars",
                ["JwtSettings:Issuer"]           = "TestIssuer",
                ["JwtSettings:Audience"]         = "TestAudience",
                ["JwtSettings:ExpiresInMinutes"] = expiresInMinutes.ToString()
            })
            .Build();
        return new JwtService(config);
    }

    private static User MakeUser(string[] roles) =>
        new User("testuser", "test@example.com", "hash", roles);

    // ─── GenerateToken ────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = BuildService().GenerateToken(MakeUser(["User"]));
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_IsValidJwtFormat()
    {
        var token = BuildService().GenerateToken(MakeUser(["User"]));
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsSubClaim_MatchingUserId()
    {
        var user = MakeUser(["User"]);
        var token = BuildService().GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var user = MakeUser(["User"]);
        var token = BuildService().GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email
                                      && c.Value == "test@example.com");
    }

    [Fact]
    public void GenerateToken_ContainsUniqueNameClaim()
    {
        var user = MakeUser(["User"]);
        var token = BuildService().GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            (c.Type == JwtRegisteredClaimNames.UniqueName || c.Type == "unique_name")
            && c.Value == "testuser");
    }

    [Fact]
    public void GenerateToken_ContainsAllRoles()
    {
        var user = MakeUser(["Admin", "User"]);
        var token = BuildService().GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var roleClaims = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        roleClaims.Should().Contain("Admin");
        roleClaims.Should().Contain("User");
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        var token = BuildService().GenerateToken(MakeUser([]));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_ExpiresAfterConfiguredMinutes()
    {
        var before = DateTime.UtcNow;
        var token = BuildService(expiresInMinutes: 30).GenerateToken(MakeUser([]));
        var after = DateTime.UtcNow;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_ContainsJtiClaim()
    {
        var token = BuildService().GenerateToken(MakeUser([]));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_TwoCallsSamUser_ProduceDifferentJti()
    {
        var sut = BuildService();
        var user = MakeUser(["User"]);
        var t1 = new JwtSecurityTokenHandler().ReadJwtToken(sut.GenerateToken(user));
        var t2 = new JwtSecurityTokenHandler().ReadJwtToken(sut.GenerateToken(user));

        var jti1 = t1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = t2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    // ─── GenerateRefreshToken ─────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = BuildService().GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_IsValidBase64()
    {
        var token = BuildService().GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_TwoCalls_ProduceUniqueTokens()
    {
        var sut = BuildService();
        var t1 = sut.GenerateRefreshToken();
        var t2 = sut.GenerateRefreshToken();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void GenerateRefreshToken_IsNotJwtFormat()
    {
        var token = BuildService().GenerateRefreshToken();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeFalse();
    }
}
