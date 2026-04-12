using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Void.Services.Interfaces;

namespace Void.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ILoggingService _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public FileStorageService(ILoggingService logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }
    
    public async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        try
        {
            if (!FileExists(path))
                return null;
                
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Erro ao ler JSON: {Path}", path);
            return null;
        }
    }
    
    public async Task WriteJsonAsync<T>(string path, T data) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(path, json);
            _logger.Info("💾 JSON salvo: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Erro ao salvar JSON: {Path}", path);
        }
    }
    
    public T? ReadJson<T>(string path) where T : class
    {
        return ReadJsonAsync<T>(path).GetAwaiter().GetResult();
    }
    
    public void WriteJson<T>(string path, T data) where T : class
    {
        WriteJsonAsync(path, data).GetAwaiter().GetResult();
    }
    
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }
    
    public void EnsureDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Info("📁 Diretório criado: {Directory}", directory);
        }
    }
}