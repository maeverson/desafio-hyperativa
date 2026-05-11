using DesafioHyperativa.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace DesafioHyperativa.Infrastructure.Security;

/// <summary>
/// Serviço de criptografia AES-256-CBC para armazenamento seguro dos números de cartão.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key não configurada.");
        var ivBase64 = configuration["Encryption:IV"]
            ?? throw new InvalidOperationException("Encryption:IV não configurada.");

        _key = Convert.FromBase64String(keyBase64);
        _iv = Convert.FromBase64String(ivBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException("A chave de criptografia deve ser de 256 bits (32 bytes).");
        if (_iv.Length != 16)
            throw new InvalidOperationException("O IV deve ser de 128 bits (16 bytes).");
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
