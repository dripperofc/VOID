using System;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

public class ChatService
{
    public bool IsConnected { get; private set; }
    
    public event EventHandler<MessageItem>? MessageReceived;
    public event EventHandler<(ChannelItem, UserProfile)>? UserTyping;
    
    public ChatService()
    {
        IsConnected = false;
    }
    
    public async Task ConnectAsync()
    {
        await Task.Delay(100);
        IsConnected = true;
    }
    
    public async Task DisconnectAsync()
    {
        await Task.Delay(100);
        IsConnected = false;
    }
    
    public async Task SendTypingIndicatorAsync(ChannelItem channel, UserProfile user)
    {
        if (!IsConnected) return;
        
        await Task.Delay(10);
        UserTyping?.Invoke(this, (channel, user));
    }
    
    public void SimulateMessageReceived(MessageItem message)
    {
        MessageReceived?.Invoke(this, message);
    }
}