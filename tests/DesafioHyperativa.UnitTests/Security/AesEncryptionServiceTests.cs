using DesafioHyperativa.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace DesafioHyperativa.UnitTests.Security;

public class AesEncryptionServiceTests
{
    // Chave AES-256 (32 bytes) e IV (16 bytes) em Base64 para testes
    private const string ValidKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="; // 32 bytes
    private const string ValidIv  = "AAAAAAAAAAAAAAAAAAAAAA==";                       // 16 bytes

    private static AesEncryptionService BuildService(string? key = ValidKey, string? iv = ValidIv)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = key,
                ["Encryption:IV"]  = iv
            })
            .Build();
        return new AesEncryptionService(config);
    }

    [Fact]
    public void Encrypt_ReturnsNonEmptyBase64()
    {
        var sut = BuildService();
        var cipher = sut.Encrypt("4111111111111111");

        cipher.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(cipher).Should().NotBeEmpty();
    }

    [Fact]
    public void Encrypt_DifferentFromPlainText()
    {
        var sut = BuildService();
        var cipher = sut.Encrypt("4111111111111111");
        cipher.Should().NotBe("4111111111111111");
    }

    [Fact]
    public void Decrypt_ReversesEncrypt()
    {
        var sut = BuildService();
        var plain = "4111111111111111";
        var cipher = sut.Encrypt(plain);
        var decrypted = sut.Decrypt(cipher);
        decrypted.Should().Be(plain);
    }

    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("5500005555555559")]
    [InlineData("378282246310005")]
    [InlineData("hello world")]
    [InlineData("")]
    public void EncryptDecrypt_RoundTrip_PreservesInput(string input)
    {
        var sut = BuildService();
        sut.Decrypt(sut.Encrypt(input)).Should().Be(input);
    }

    [Fact]
    public void Encrypt_SameInputSameKey_ReturnsSameCipher()
    {
        var sut = BuildService();
        var c1 = sut.Encrypt("4111111111111111");
        var c2 = sut.Encrypt("4111111111111111");
        // AES-CBC com IV fixo deve produzir o mesmo cipher para o mesmo input
        c1.Should().Be(c2);
    }

    [Fact]
    public void Encrypt_DifferentInputs_ProduceDifferentCiphers()
    {
        var sut = BuildService();
        var c1 = sut.Encrypt("4111111111111111");
        var c2 = sut.Encrypt("5500005555555559");
        c1.Should().NotBe(c2);
    }

    [Fact]
    public void Constructor_MissingKey_ThrowsInvalidOperation()
    {
        var act = () => BuildService(key: null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption:Key*");
    }

    [Fact]
    public void Constructor_MissingIv_ThrowsInvalidOperation()
    {
        var act = () => BuildService(iv: null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption:IV*");
    }

    [Fact]
    public void Constructor_KeyWrongLength_ThrowsInvalidOperation()
    {
        // 16 bytes = 128 bits (inválido para AES-256)
        var shortKey = Convert.ToBase64String(new byte[16]);
        var act = () => BuildService(key: shortKey);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*256 bits*");
    }

    [Fact]
    public void Constructor_IvWrongLength_ThrowsInvalidOperation()
    {
        var shortIv = Convert.ToBase64String(new byte[8]);
        var act = () => BuildService(iv: shortIv);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*128 bits*");
    }
}
