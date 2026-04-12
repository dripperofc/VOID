using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

public class ChatService
{
    private HubConnection? _connection;

    public event Action<MessageItem>? MessageReceived;
    public event Action<MessageItem>? PrivateMessageReceived;
    public event Action<string, bool>? OnUserStatusChanged;
    public event Action<string>? ConnectionFailed;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string userId)
    {
        if (_connection?.State == HubConnectionState.Connected) return;

        if (_connection != null) { await _connection.DisposeAsync(); _connection = null; }

        _connection = new HubConnectionBuilder()
            .WithUrl($"http://localhost:5159/voidchat?userId={userId}")
            .WithAutomaticReconnect()
            .Build();

        // Mensagens de canal (servidor)
        _connection.On<object>("ReceiveMessage", (raw) =>
        {
            var msg = ParseMessage(raw);
            if (msg != null) MessageReceived?.Invoke(msg);
        });

        // Mensagens diretas (DM)
        _connection.On<object>("ReceivePrivateMessage", (raw) =>
        {
            var msg = ParseMessage(raw);
            if (msg != null) PrivateMessageReceived?.Invoke(msg);
        });

        _connection.On<string, bool>("UserStatusChanged", (id, isOnline) =>
            OnUserStatusChanged?.Invoke(id, isOnline));

        try { await _connection.StartAsync(); }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Erro: {ex.Message}");
            ConnectionFailed?.Invoke(ex.Message);
        }
    }

    public async Task JoinChannelAsync(string serverId, string channelId)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("JoinChannel", serverId, channelId);
    }

    public async Task SendMessageAsync(MessageItem message, string serverId, string channelId)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("SendMessage",
                message.Author?.Username ?? "?",
                message.Content,
                "#FFFFFF", "",
                serverId, channelId);
    }

    public async Task SendPrivateMessageAsync(string targetUserId, string message, string fromUsername)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("SendPrivateMessage", targetUserId, message, fromUsername);
    }

    public void SimulateMessageReceived(MessageItem message) => MessageReceived?.Invoke(message);

    public async Task DisconnectAsync()
    {
        if (_connection != null) { await _connection.DisposeAsync(); _connection = null; }
    }

    private static MessageItem? ParseMessage(object raw)
    {
        try
        {
            if (raw is System.Text.Json.JsonElement je)
            {
                var author = new UserProfile
                {
                    Username = je.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "",
                    Nickname = je.TryGetProperty("username", out var n) ? n.GetString() ?? "" : "",
                };
                author.Initials = author.Nickname.Length > 0 ? author.Nickname[0].ToString().ToUpper() : "?";

                return new MessageItem
                {
                    Content = je.TryGetProperty("message", out var c) ? c.GetString() ?? "" : "",
                    Author = author,
                    Timestamp = je.TryGetProperty("timestamp", out var t) && t.TryGetDateTime(out var dt) ? dt.ToLocalTime() : DateTime.Now
                };
            }
        }
        catch { }
        return null;
    }
}