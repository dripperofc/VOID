using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

// Classe interna para salvar/ler o arquivo JSON com senha hash
file class StoredAccount
{
    public UserProfile Profile { get; set; } = new();
    public string PasswordHash { get; set; } = string.Empty; // formato "salt$hash"
}

public class AuthenticationService
{
    private readonly SecurityService _security = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public UserProfile? CurrentUser { get; private set; }

    public async Task<UserProfile?> LoginAsync(string username, string password)
    {
        try
        {
            var accountPath = $"Accounts/{username.ToLower()}.json";
            if (!File.Exists(accountPath)) return null;

            var stored = JsonSerializer.Deserialize<StoredAccount>(
                await File.ReadAllTextAsync(accountPath), _jsonOptions);
            if (stored == null) return null;

            // Verificação com SHA-256 + salt (VerifyPassword suporta legado também)
            if (!_security.VerifyPassword(password, stored.PasswordHash))
                return null;

            CurrentUser = stored.Profile;
            CurrentUser.LastLogin = DateTime.Now;

            // Atualiza LastLogin mantendo hash intacto
            stored.Profile = CurrentUser;
            await File.WriteAllTextAsync(accountPath,
                JsonSerializer.Serialize(stored, _jsonOptions));

            return CurrentUser;
        }
        catch { return null; }
    }

    public async Task<UserProfile?> RegisterAsync(string username, string nickname, string password)
    {
        try
        {
            var accountPath = $"Accounts/{username.ToLower()}.json";
            if (File.Exists(accountPath)) return null;
            Directory.CreateDirectory("Accounts");

            var nextId = GetNextUserId();
            var profile = new UserProfile
            {
                Id        = nextId,
                Username  = username,
                Nickname  = string.IsNullOrWhiteSpace(nickname) ? username : nickname,
                CreatedAt = DateTime.Now,
                LastLogin = DateTime.Now,
                AvatarColor = "#5865F2",
                Initials  = username.Length >= 2 ? username[..2].ToUpper() : username.ToUpper(),
                IsOwner   = username.ToLower() is "admin" or "dono"
            };

            var stored = new StoredAccount
            {
                Profile      = profile,
                PasswordHash = _security.HashPassword(password) // SHA-256 + salt
            };

            await File.WriteAllTextAsync(accountPath,
                JsonSerializer.Serialize(stored, _jsonOptions));

            CurrentUser = profile;
            return profile;
        }
        catch { return null; }
    }

    public Task<bool> LogoutAsync()
    {
        CurrentUser = null;
        return Task.FromResult(true);
    }

    private int GetNextUserId()
    {
        const string lockFile = "last_id.txt";
        if (!File.Exists(lockFile)) { File.WriteAllText(lockFile, "1000"); return 1000; }
        var content = File.ReadAllText(lockFile);
        if (int.TryParse(content, out int id))
        {
            File.WriteAllText(lockFile, (id + 1).ToString());
            return id + 1;
        }
        return 1000;
    }
}
