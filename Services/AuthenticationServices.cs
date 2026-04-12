using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

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
            var account = JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
            
            if (account == null)
                return null;
            
            // Verificação simples de senha (depois melhorar com hash)
            if (account.Password != password)
                return null;
            
            CurrentUser = account;
            CurrentUser.LastLogin = DateTime.Now;
            
            // Atualizar último login
            var updatedJson = JsonSerializer.Serialize(CurrentUser, _jsonOptions);
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
            
            // Garantir que diretório existe
            Directory.CreateDirectory("Accounts");
            
            // Gerar próximo ID
            var nextId = GetNextUserId();
            
            var profile = new UserProfile
            {
                Id = nextId,
                Username = username,
                Nickname = string.IsNullOrWhiteSpace(nickname) ? username : nickname,
                Password = password,
                CreatedAt = DateTime.Now,
                LastLogin = DateTime.Now,
                AvatarColor = "#5865F2",
                Initials = username.Length >= 2 ? username[..2].ToUpper() : username.ToUpper(),
                IsOwner = username.ToLower() == "admin" || username.ToLower() == "dono"
            };
            
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
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