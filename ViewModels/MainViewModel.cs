using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Void.Models;

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ServerItem _currentServer;
    
    [ObservableProperty]
    private ChannelItem _currentChannel;
    
    [ObservableProperty]
    private UserProfile _currentUser;
    
    [ObservableProperty]
    private string _messageInput = string.Empty;
    
    [ObservableProperty]
    private bool _isInCall;
    
    public ObservableCollection<ServerItem> Servers { get; set; } = new();
    public ObservableCollection<ChannelItem> TextChannels { get; set; } = new();
    public ObservableCollection<ChannelItem> VoiceChannels { get; set; } = new();
    public ObservableCollection<FriendItem> Friends { get; set; } = new();
    public ObservableCollection<MessageItem> Messages { get; set; } = new();
    
    public string OnlineMembersHeader => "ONLINE — 1";
    public string OfflineMembersHeader => "OFFLINE — 0";
    
    public MainViewModel()
    {
        _currentServer = new ServerItem { Name = "Carregando..." };
        _currentChannel = new ChannelItem { Name = "geral" };
        _currentUser = new UserProfile { Username = "Usuário", Nickname = "Usuário" };
        
        InitializeData();
    }
    
    private void InitializeData()
    {
        CurrentUser = new UserProfile
        {
            Id = 1,
            Username = "Admin",
            Nickname = "Admin",
            Initials = "AD",
            IsOwner = true,
            IsOnline = true,
            AvatarColor = "#5865F2"
        };
        
        var officialServer = new ServerItem
        {
            Id = 1,
            Name = "Servidor do Void",
            IsOfficial = true
        };
        
        // Adicionar canais manualmente
        officialServer.Channels.Add(new ChannelItem 
        { 
            Id = 1, 
            ServerId = 1,
            Name = "geral", 
            Type = ChannelType.Text, 
            Topic = "Bate-papo geral" 
        });
        
        officialServer.Channels.Add(new ChannelItem 
        { 
            Id = 2, 
            ServerId = 1,
            Name = "voz", 
            Type = ChannelType.Voice 
        });
        
        Servers.Add(officialServer);
        CurrentServer = officialServer;
        
        LoadChannels();
        
        // Amigos
        Friends.Add(new FriendItem
        {
            Id = 1,
            Name = "Amigo1",
            Status = "Online",
            Initials = "AM",
            AvatarColor = "#23A55A"
        });
        
        Messages.Add(new MessageItem
        {
            Author = CurrentUser,
            Content = "Bem-vindo ao Void! 👋"
        });
    }
    
    private void LoadChannels()
    {
        TextChannels.Clear();
        VoiceChannels.Clear();
        
        if (CurrentServer?.Channels == null) return;
        
        foreach (var channel in CurrentServer.Channels)
        {
            if (channel.Type == ChannelType.Text)
                TextChannels.Add(channel);
            else if (channel.Type == ChannelType.Voice)
                VoiceChannels.Add(channel);
        }
        
        if (TextChannels.Count > 0)
            CurrentChannel = TextChannels[0];
    }
    
    [RelayCommand]
    private void SelectServer(ServerItem? server)
    {
        if (server == null) return;
        CurrentServer = server;
        LoadChannels();
    }
    
    [RelayCommand]
    private void SelectChannel(ChannelItem? channel)
    {
        if (channel == null) return;
        CurrentChannel = channel;
        Messages.Clear();
        Messages.Add(new MessageItem
        {
            Author = CurrentUser,
            Content = $"📌 Entrou em #{channel.Name}"
        });
    }
    
    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;
        
        Messages.Add(new MessageItem
        {
            Author = CurrentUser,
            Content = MessageInput
        });
        
        MessageInput = string.Empty;
    }
    
    [RelayCommand]
    private void ToggleMicrophone()
    {
        IsInCall = !IsInCall;
    }
    
    [RelayCommand]
    private void OpenSettings() { }
    
    [RelayCommand]
    private void AddFriend() { }
    
    [RelayCommand]
    private void CreateServer()
    {
        var newServer = new ServerItem
        {
            Id = Servers.Count + 1,
            Name = $"Servidor {Servers.Count + 1}"
        };
        
        newServer.Channels.Add(new ChannelItem 
        { 
            Id = 1, 
            ServerId = newServer.Id,
            Name = "geral", 
            Type = ChannelType.Text 
        });
        
        Servers.Add(newServer);
        CurrentServer = newServer;
        LoadChannels();
    }
}