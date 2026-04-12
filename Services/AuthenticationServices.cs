using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

// Classe interna usada APENAS para salvar/ler o arquivo JSON com senha
// Nunca deve circular pelo resto da aplicação
file class StoredAccount
{
    public UserProfile Profile { get; set; } = new();
    public string PasswordHash { get; set; } = string.Empty;
}

public class AuthenticationService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public UserProfile? CurrentUser { get; private set; }

    public AuthenticationService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<UserProfile?> LoginAsync(string username, string password)
    {
        try
        {
            var accountPath = $"Accounts/{username.ToLower()}.json";

            if (!File.Exists(accountPath))
                return null;

            var json = await File.ReadAllTextAsync(accountPath);
            var stored = JsonSerializer.Deserialize<StoredAccount>(json, _jsonOptions);

            if (stored == null)
                return null;

            // Verificação de senha (depois melhorar com hash bcrypt)
            if (stored.PasswordHash != password)
                return null;

            CurrentUser = stored.Profile;
            CurrentUser.LastLogin = DateTime.Now;

            // Atualizar último login mantendo a senha no arquivo
            stored.Profile = CurrentUser;
            var updatedJson = JsonSerializer.Serialize(stored, _jsonOptions);
            await File.WriteAllTextAsync(accountPath, updatedJson);

            return CurrentUser;
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserProfile?> RegisterAsync(string username, string nickname, string password)
    {
        try
        {
            var accountPath = $"Accounts/{username.ToLower()}.json";

            if (File.Exists(accountPath))
                return null;

            Directory.CreateDirectory("Accounts");

            var nextId = GetNextUserId();

            var profile = new UserProfile
            {
                Id = nextId,
                Username = username,
                Nickname = string.IsNullOrWhiteSpace(nickname) ? username : nickname,
                CreatedAt = DateTime.Now,
                LastLogin = DateTime.Now,
                AvatarColor = "#5865F2",
                Initials = username.Length >= 2 ? username[..2].ToUpper() : username.ToUpper(),
                IsOwner = username.ToLower() == "admin" || username.ToLower() == "dono"
            };

            // Senha fica isolada no StoredAccount, nunca no UserProfile
            var stored = new StoredAccount
            {
                Profile = profile,
                PasswordHash = password // TODO: substituir por bcrypt hash
            };

            var json = JsonSerializer.Serialize(stored, _jsonOptions);
            await File.WriteAllTextAsync(accountPath, json);

            CurrentUser = profile;
            return profile;
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> LogoutAsync()
    {
        CurrentUser = null;
        return Task.FromResult(true);
    }

    private int GetNextUserId()
    {
        var lockFile = "last_id.txt";

        if (!File.Exists(lockFile))
        {
            File.WriteAllText(lockFile, "1000");
            return 1000;
        }

        var content = File.ReadAllText(lockFile);
        if (int.TryParse(content, out int currentId))
        {
            var nextId = currentId + 1;
            File.WriteAllText(lockFile, nextId.ToString());
            return nextId;
        }

        return 1000;
    }
}