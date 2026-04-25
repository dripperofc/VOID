using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Void.Services;

/// <summary>
/// Segurança: SHA-256 + salt para senhas, AES-256-CBC para criptografia E2E de mensagens.
/// </summary>
public class SecurityService
{
    // ── HASHING DE SENHA (SHA-256 + salt) ────────────────────────────────

    /// <summary>Retorna "salt$hash" onde salt é 16 bytes hex e hash é SHA-256(salt+password) hex.</summary>
    public string HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToHexString(saltBytes).ToLower();
        var hash = ComputeSha256(salt + password);
        return $"{salt}${hash}";
    }

    /// <summary>Verifica senha contra hash armazenado no formato "salt$hash" ou legado base64.</summary>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        var parts = storedHash.Split('$');
        if (parts.Length == 2)
        {
            var actual = ComputeSha256(parts[0] + password);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(actual),
                Encoding.UTF8.GetBytes(parts[1]));
        }

        // Fallback legado: hash direto sem salt
        var legacy = ComputeSha256Base64(password);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacy),
            Encoding.UTF8.GetBytes(storedHash));
    }

    public string GenerateToken(int length = 32)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));

    // ── CRIPTOGRAFIA E2E (AES-256-CBC) ────────────────────────────────────

    /// <summary>
    /// Deriva chave AES-256 simétrica entre dois usuários.
    /// Determinístico: SHA-256("void_e2e_v1:sorted(userA:userB)").
    /// </summary>
    public byte[] DeriveSharedKey(string userA, string userB)
    {
        var pair = string.Compare(userA, userB, StringComparison.Ordinal) <= 0
            ? $"{userA}:{userB}" : $"{userB}:{userA}";
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes("void_e2e_v1:" + pair));
    }

    /// <summary>Cifra mensagem. Retorna Base64(IV[16] + CipherText).</summary>
    public string Encrypt(string plainText, byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Chave deve ser 32 bytes.");
        using var aes = Aes.Create();
        aes.Key = key; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, 16);
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
            sw.Write(plainText);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>Decifra Base64(IV[16] + CipherText) produzido por Encrypt.</summary>
    public string Decrypt(string cipherBase64, byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Chave deve ser 32 bytes.");
        var full = Convert.FromBase64String(cipherBase64);
        using var aes = Aes.Create();
        aes.Key = key; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        var iv = new byte[16]; var ct = new byte[full.Length - 16];
        Buffer.BlockCopy(full, 0, iv, 0, 16);
        Buffer.BlockCopy(full, 16, ct, 0, ct.Length);
        aes.IV = iv;
        using var ms = new MemoryStream(ct);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input))).ToLower();
    }
    private static string ComputeSha256Base64(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}
