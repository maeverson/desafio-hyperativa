using System.Security.Cryptography;
using System.Text;

namespace DesafioHyperativa.Shared.Helpers;

public static class CardHashHelper
{
    /// <summary>
    /// Gera hash SHA-256 do número do cartão normalizado.
    /// Usado para busca/indexação sem expor o número real.
    /// </summary>
    public static string ComputeHash(string cardNumber)
    {
        var normalized = cardNumber.Trim().Replace(" ", "").Replace("-", "");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Remove espaços, traços e valida que são apenas dígitos.
    /// </summary>
    public static string Normalize(string cardNumber)
        => cardNumber.Trim().Replace(" ", "").Replace("-", "");

    /// <summary>
    /// Valida número do cartão usando o algoritmo de Luhn.
    /// </summary>
    public static bool IsValidLuhn(string cardNumber)
    {
        var digits = Normalize(cardNumber);
        if (string.IsNullOrEmpty(digits) || !digits.All(char.IsDigit))
            return false;

        var sum = 0;
        var isEven = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (isEven)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            isEven = !isEven;
        }
        return sum % 10 == 0;
    }
}
