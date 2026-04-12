using System;
using System.Security.Cryptography;
using System.Text;

namespace Void.Services;

public class SecurityService
{
    public string HashPassword(string password)
    {
        // Implementação simples de hash (SHA256)
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
    
    public string GenerateToken(int length = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(bytes);
    }
}