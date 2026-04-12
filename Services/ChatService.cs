using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Void.Models;

namespace Void.Services;

public class ChatService
{
    private HubConnection? _connection;

    // Renomeei para bater com o que o seu ViewModel procura
    public event Action<MessageItem>? MessageReceived;
    public event Action<string, bool>? OnUserStatusChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string userId)
    {
        if (_connection != null) await _connection.DisposeAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"http://localhost:5159/voidchat?userId={userId}")
            .WithAutomaticReconnect()
            .Build();

        // Quando o servidor mandar "ReceiveMessage", a gente dispara o evento
        _connection.On<MessageItem>("ReceiveMessage", (msg) => MessageReceived?.Invoke(msg));
        
        _connection.On<string, bool>("UserStatusChanged", (id, isOnline) => OnUserStatusChanged?.Invoke(id, isOnline));

        try { await _connection.StartAsync(); }
        catch (Exception ex) { Console.WriteLine($"Erro: {ex.Message}"); }
    }

    public async Task SendMessageAsync(MessageItem message, string serverId, string channelId)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("SendMessage", message.Author?.Nickname, message.Content, "#FFF", "", serverId, channelId);
    }

    // Criado apenas para não dar erro no ViewModel enquanto testamos
    public void SimulateMessageReceived(MessageItem message)
    {
        MessageReceived?.Invoke(message);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
    }
}