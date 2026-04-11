using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Void.Services;

public class ChatService
{
    private HubConnection? _connection;

    public event Action<ChatMessage>? OnMessageReceived;
    public event Action<List<ChatMessage>>? OnHistoryLoaded;
    public event Action<ChatMessage>? OnPrivateMessageReceived;
    public event Action<string, bool>? OnUserStatusChanged;
    public event Action<string>? OnConnectionError;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string userId)
    {
        if (_connection != null)
            await _connection.DisposeAsync();

        // ⚠️ ATENÇÃO: Se o seu servidor rodar em outra porta, mude o 5159 aqui!
        _connection = new HubConnectionBuilder()
            .WithUrl($"http://localhost:5159/voidchat?userId={userId}")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ChatMessage>("ReceiveMessage", (msg) =>
        {
            OnMessageReceived?.Invoke(msg);
        });

        _connection.On<List<ChatMessage>>("LoadHistory", (messages) =>
        {
            OnHistoryLoaded?.Invoke(messages);
        });

        _connection.On<ChatMessage>("ReceivePrivateMessage", (msg) =>
        {
            OnPrivateMessageReceived?.Invoke(msg);
        });

        _connection.On<string, bool>("UserStatusChanged", (id, isOnline) =>
        {
            OnUserStatusChanged?.Invoke(id, isOnline);
        });

        try
        {
            await _connection.StartAsync();
            Console.WriteLine("[VOID NETWORK] Connected");
        }
        catch (Exception ex)
        {
            OnConnectionError?.Invoke(ex.Message);
        }
    }

    public async Task JoinChannelAsync(string serverId, string channelId)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("JoinChannel", serverId, channelId);
    }

    public async Task SendMessageAsync(string username, string message, string color, string badge, string serverId, string channelId)
    {
        if (!IsConnected) throw new Exception("Você não está conectado ao servidor!");
        
        // Sem try-catch aqui, deixa o erro subir pro ViewModel avisar na tela
        await _connection!.InvokeAsync("SendMessage", username, message, color, badge, serverId, channelId);
    }

    public async Task SendPrivateMessageAsync(string targetUserId, string message, string username)
    {
        if (!IsConnected) throw new Exception("Você não está conectado ao servidor!");
        await _connection!.InvokeAsync("SendPrivateMessage", targetUserId, message, username);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}

public class ChatMessage
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Message { get; set; } = "";
    public string Color { get; set; } = "";
    public string Badge { get; set; } = "";
    
    // 🔥 O SEGREDO: DateTime anulável (?) resolve os crashes do SignalR
    public DateTime? Timestamp { get; set; } 
    
    public string ServerId { get; set; } = "";
    public string ChannelId { get; set; } = "";
}