using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models; 

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _input = "";
    [ObservableProperty] private string _currentChatName = "Amigo Exemplo";
    [ObservableProperty] private string _newServerName = "";
    
    [ObservableProperty] private bool _isCreatingServer = false;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isShowingFriends = true;
    [ObservableProperty] private bool _showProfileSettings = false;
    [ObservableProperty] private bool _showDeleteWarning = false;
    [ObservableProperty] private bool _notificationsEnabled = true;

    private ServerItem? _serverToHeader;
    public UserProfile CurrentUser { get; } = new();

    public ObservableCollection<MessageItem> ChatMessages { get; } = new();
    public ObservableCollection<FriendItem> Friends { get; } = new();
    public ObservableCollection<ServerItem> Servers { get; } = new();
    public ObservableCollection<string> CurrentChannels { get; } = new();

    public MainViewModel()
    {
        Friends.Add(new FriendItem { Name = "Amigo Exemplo", Status = "Disponível" });
        var voidServer = new ServerItem { Name = "Void" };
        voidServer.Channels.Add("geral");
        voidServer.Channels.Add("comandos");
        Servers.Add(voidServer);
    }

    // --- COMANDOS DO ROADMAP (CALLS E NOTIFICAÇÕES) ---
    [RelayCommand] public void ToggleNotifications() => NotificationsEnabled = !NotificationsEnabled;
    
    [RelayCommand] 
    public void StartVoiceCall() 
    {
        // Aqui vai entrar a lógica da Tela de Call de Voz no futuro!
        ChatMessages.Add(new MessageItem { Author = "Sistema", Content = "📞 Iniciando chamada de voz..." });
    }

    [RelayCommand] 
    public void StartVideoCall() 
    {
        // Aqui vai entrar a lógica da Tela de Call de Vídeo!
        ChatMessages.Add(new MessageItem { Author = "Sistema", Content = "📹 Iniciando chamada de vídeo..." });
    }

    // --- COMANDOS DE INTERFACE ---
    [RelayCommand] public void OpenProfileSettings() => ShowProfileSettings = true;
    [RelayCommand] public void CloseProfileSettings() => ShowProfileSettings = false;
    [RelayCommand] public void CancelDelete() => ShowDeleteWarning = false;
    [RelayCommand] public void ToggleCreateServer() => IsCreatingServer = !IsCreatingServer;

    [RelayCommand]
    public void RequestDeleteServer(ServerItem server) { _serverToHeader = server; ShowDeleteWarning = true; }

    [RelayCommand]
    public void ConfirmDeleteServer() { if (_serverToHeader != null) { Servers.Remove(_serverToHeader); GoToDms(); } ShowDeleteWarning = false; }

    [RelayCommand]
    public void SelectServer(ServerItem s) 
    { 
        IsInServer = true; IsShowingFriends = false; CurrentChannels.Clear(); 
        foreach(var c in s.Channels) CurrentChannels.Add(c); 
        CurrentChatName = s.Channels[0]; // Tirei o "#" daqui, o XAML cuida disso agora
    }

    [RelayCommand] 
    public void GoToDms() 
    { 
        IsInServer = false; IsShowingFriends = true; CurrentChatName = "Amigo Exemplo"; 
    }

    [RelayCommand] 
    public void ConfirmCreateServer() 
    { 
        if(!string.IsNullOrWhiteSpace(NewServerName)){ 
            var s = new ServerItem { Name = NewServerName }; 
            s.Channels.Add("geral"); Servers.Add(s); NewServerName = ""; IsCreatingServer = false; 
        } 
    }

    [RelayCommand] 
    public void ProcessMessage() 
    { 
        if(string.IsNullOrWhiteSpace(Input)) return; 
        var msg = new MessageItem { Author = CurrentUser.Name, Content = Input, NameColor = CurrentUser.Color, Badge = CurrentUser.Badge }; 
        Dispatcher.UIThread.Post(() => { ChatMessages.Add(msg); Input = ""; }); 
    }

    [RelayCommand] 
    public void SelectFriend(FriendItem friend) 
    { 
        IsInServer = false; IsShowingFriends = true; CurrentChatName = friend.Name; ChatMessages.Clear(); 
    }
}