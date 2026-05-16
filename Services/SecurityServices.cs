using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Void.Services;

/// <summary>
/// Segurança: BCrypt para senhas, AES-256-CBC para criptografia (não é E2E real — servidor tem a chave).
/// </summary>
public class SecurityService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(password, storedHash); }
        catch { return false; }
    }

    public string GenerateToken(int length = 32)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));
}
