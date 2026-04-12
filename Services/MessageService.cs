using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

public class MessageService
{
    private readonly JsonSerializerOptions _jsonOptions;
    
    public MessageService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }
    
    public async Task SendMessageAsync(ChannelItem channel, UserProfile author, string content)
    {
        try
        {
            var message = new MessageItem
            {
                Id = Guid.NewGuid().ToString(),
                Author = author,
                Content = content,
                Timestamp = DateTime.Now
            };
            
            var historyPath = GetHistoryPath(channel);
            Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);
            
            var messages = await LoadMessageHistoryAsync(channel);
            messages.Add(message);
            
            var json = JsonSerializer.Serialize(messages, _jsonOptions);
            await File.WriteAllTextAsync(historyPath, json);
        }
        catch
        {
            // Ignorar erros de persistência por enquanto
        }
    }
    
    public async Task<ObservableCollection<MessageItem>> LoadMessageHistoryAsync(ChannelItem channel, int limit = 50)
    {
        try
        {
            var historyPath = GetHistoryPath(channel);
            
            if (!File.Exists(historyPath))
                return new ObservableCollection<MessageItem>();
            
            var json = await File.ReadAllTextAsync(historyPath);
            var allMessages = JsonSerializer.Deserialize<ObservableCollection<MessageItem>>(json, _jsonOptions);
            
            if (allMessages == null)
                return new ObservableCollection<MessageItem>();
                
            var recentMessages = allMessages
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToList();
                
            return new ObservableCollection<MessageItem>(recentMessages);
        }
        catch
        {
            return new ObservableCollection<MessageItem>();
        }
    }
    
    public Task DeleteMessageAsync(MessageItem message)
    {
        message.IsDeleted = true;
        message.Content = "[Mensagem apagada]";
        return Task.CompletedTask;
    }
    
    public Task EditMessageAsync(MessageItem message, string newContent)
    {
        message.Content = newContent;
        message.EditedAt = DateTime.Now;
        return Task.CompletedTask;
    }
    
    private string GetHistoryPath(ChannelItem channel)
    {
        return $"servers/server_{channel.ServerId}/channel_{channel.Id}/messages.json";
    }
}