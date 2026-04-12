using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models; 
using Void.Services;
using NAudio.Wave;

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // --- ESTADOS DE INTERFACE ---
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _showWelcomePopup = false;
    [ObservableProperty] private bool _isShowingFriends = true;
    [ObservableProperty] private bool _isInServer = false;

    // --- PROPRIEDADES DE ÁUDIO ---
    private bool _isMuted = false;
    public bool IsMuted { get => _isMuted; set { SetProperty(ref _isMuted, value); OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor)); } }

    private bool _isDeafened = false;
    public bool IsDeafened { get => _isDeafened; set { SetProperty(ref _isDeafened, value); OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor)); } }

    public string MuteIcon => IsMuted ? "🙊" : "🎤";
    public string DeafenIcon => IsDeafened ? "🔇" : "🎧";
    public string MuteColor => IsMuted ? "#ED4245" : "#B5BAC1";
    public string DeafenColor => IsDeafened ? "#ED4245" : "#B5BAC1";

    // --- INPUTS ---
    [ObservableProperty] private string _usernameInput = "";
    [ObservableProperty] private string _nicknameInput = "";
    [ObservableProperty] private string _passwordInput = "";
    [ObservableProperty] private string _welcomeMessage = "";
    [ObservableProperty] private string _input = "";
    [ObservableProperty] private string _currentChatName = "Amigos";
    [ObservableProperty] private string _friendNameInput = "";

    public UserProfile CurrentUser { get; } = new UserProfile();
    private readonly ChatService _chatService = new ChatService(); 
    
    public ObservableCollection<MessageItem> ChatMessages { get; } = new ObservableCollection<MessageItem>();
    public ObservableCollection<FriendItem> Friends { get; } = new ObservableCollection<FriendItem>(); 
    public ObservableCollection<ServerItem> Servers { get; } = new ObservableCollection<ServerItem>();
    public ObservableCollection<ChannelItem> CurrentChannels { get; } = new ObservableCollection<ChannelItem>();

    public string WindowTitle => IsInServer ? "Void - Chat" : "Void - DMs";

    public MainViewModel()
    {
        _chatService.MessageReceived += (msg) => Dispatcher.UIThread.Post(() => {
            if (msg != null) ChatMessages.Add(msg);
        });
    }

    // --- COMANDOS DE INTERFACE ---
    [RelayCommand]
    public void OpenSettings() { /* Futura lógica de configurações */ }

    [RelayCommand] public void ToggleMute() => IsMuted = !IsMuted;
    
    [RelayCommand] public void ToggleDeafen() { IsDeafened = !IsDeafened; if(IsDeafened) IsMuted = true; }
    
    [RelayCommand] public void GoToDms() { IsInServer = false; CurrentChatName = "Amigos"; ChatMessages.Clear(); }
    
    [RelayCommand]
    public void ToggleView() 
    { 
        IsRegisterView = !IsRegisterView; 
        UsernameInput = "";
        PasswordInput = "";
        NicknameInput = "";
        OnPropertyChanged(nameof(WindowTitle));
    }

    // --- COMANDOS DE SESSÃO ---
    [RelayCommand]
    public async Task ConfirmLogin() { 
        CurrentUser.Nickname = string.IsNullOrEmpty(UsernameInput) ? "User" : UsernameInput; 
        IsLoggedIn = true; 
        await _chatService.ConnectAsync(CurrentUser.Nickname);
        GoToDms();
    }

    [RelayCommand]
    public async Task ConfirmRegister()
    {
        // Define o Nickname (usa o Username se o Nickname estiver vazio)
        CurrentUser.Nickname = string.IsNullOrEmpty(NicknameInput) ? UsernameInput : NicknameInput;
        IsLoggedIn = true;
        await _chatService.ConnectAsync(CurrentUser.Nickname);
        GoToDms();
    }

    // --- COMANDOS DE SOCIAL/CHAT ---
    [RelayCommand] 
    public void AddFriend() { 
        if(!string.IsNullOrEmpty(FriendNameInput)) { 
            Friends.Add(new FriendItem { Name = FriendNameInput }); 
            FriendNameInput = ""; 
        } 
    }

    [RelayCommand]
    public void SelectServer(ServerItem s) { 
        if (s == null) return;
        IsInServer = true;
        CurrentChannels.Clear();
        
        if(s.Channels != null) 
        {
            foreach(var c in s.Channels) 
            {
                CurrentChannels.Add(c);
            }
        }

        var first = CurrentChannels.FirstOrDefault();
        if (first != null) SelectChannel(first);
    }

    [RelayCommand] 
    public void SelectChannel(ChannelItem c) 
    { 
        if (c == null) return;
        CurrentChatName = c.Name; 
    }

    [RelayCommand] public void SelectFriend(FriendItem f) { if(f != null) { IsInServer = false; CurrentChatName = f.Name; ChatMessages.Clear(); } }

    [RelayCommand]
    public async Task ProcessMessage() 
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        
        var newMessage = new MessageItem();
        newMessage.Content = Input;
        newMessage.Author = this.CurrentUser; 
        newMessage.Timestamp = DateTime.Now; 

        Input = "";
        
        if (_chatService.IsConnected)
        {
            await _chatService.SendMessageAsync(newMessage, "global", "geral");
        }
        else
        {
            _chatService.SimulateMessageReceived(newMessage);
        }
        
        await Task.CompletedTask;
    }
}