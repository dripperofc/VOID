using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services.Interfaces;

// ===== SERVIÇOS CORE =====
public interface ILoggingService
{
    void Info(string message, params object[] args);
    void Warning(string message, params object[] args);
    void Error(string message, params object[] args);
    void Error(Exception ex, string message, params object[] args);
}

public interface ISecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateToken(int length = 32);
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface IFileStorageService
{
    Task<T?> ReadJsonAsync<T>(string path) where T : class;
    Task WriteJsonAsync<T>(string path, T data) where T : class;
    T? ReadJson<T>(string path) where T : class;
    void WriteJson<T>(string path, T data) where T : class;
    bool FileExists(string path);
    void EnsureDirectoryExists(string path);
}

// ===== SERVIÇOS DE NEGÓCIO =====
public interface IAuthenticationService
{
    Task<UserProfile?> LoginAsync(string username, string password);
    Task<UserProfile?> RegisterAsync(string username, string nickname, string password);
    Task<bool> LogoutAsync();
    UserProfile? CurrentUser { get; }
}

public interface IServerService
{
    Task<ObservableCollection<ServerItem>> LoadServersAsync();
    Task<ServerItem> CreateServerAsync(string name, UserProfile owner);
    Task JoinServerAsync(ServerItem server, UserProfile user);
    Task LeaveServerAsync(ServerItem server, UserProfile user);
    ServerItem GetOfficialServer();
}

public interface IMessageService
{
    Task SendMessageAsync(ChannelItem channel, UserProfile author, string content);
    Task<ObservableCollection<MessageItem>> LoadMessageHistoryAsync(ChannelItem channel, int limit = 50);
    Task DeleteMessageAsync(MessageItem message);
    Task EditMessageAsync(MessageItem message, string newContent);
}

public interface IAudioService
{
    Task StartVoiceCaptureAsync();
    Task StopVoiceCaptureAsync();
    Task StartAudioPlaybackAsync(byte[] audioData);
    void SetVolume(float volume);
    bool IsCapturing { get; }
    bool IsPlaying { get; }
    event EventHandler<byte[]> AudioDataReceived;
}

public interface IChatService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendTypingIndicatorAsync(ChannelItem channel, UserProfile user);
    bool IsConnected { get; }
    event EventHandler<MessageItem> MessageReceived;
    event EventHandler<(ChannelItem, UserProfile)> UserTyping;
}