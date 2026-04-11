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

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // --- PROPRIEDADES DE ESTADO (OBSERVABLE) ---
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _showWelcomePopup = false;
    [ObservableProperty] private bool _isShowingFriends = true;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isCreatingServer = false;
    [ObservableProperty] private bool _isInCall = false;

    // --- INPUTS ---
    [ObservableProperty] private string _usernameInput = "";
    [ObservableProperty] private string _nicknameInput = "";
    [ObservableProperty] private string _passwordInput = "";
    [ObservableProperty] private string _welcomeMessage = "";
    [ObservableProperty] private string _input = "";
    [ObservableProperty] private string _currentChatName = "Amigos";
    [ObservableProperty] private string _friendNameInput = "";
    [ObservableProperty] private string _newServerName = "";

    // --- DADOS ---
    public UserProfile CurrentUser { get; } = new();
    private readonly ChatService _chatService = new(); 
    private readonly string _accountsFolder = "Accounts";
    private readonly string _idFilePath = "last_id.txt";
    private readonly string _officialServerPath = "official_server.json";

    public ObservableCollection<MessageItem> ChatMessages { get; } = new();
    public ObservableCollection<FriendItem> Friends { get; } = new(); 
    public ObservableCollection<ServerItem> Servers { get; } = new();
    public ObservableCollection<string> CurrentChannels { get; } = new();

    public MainViewModel()
    {
        if (!Directory.Exists(_accountsFolder)) Directory.CreateDirectory(_accountsFolder);
        if (!File.Exists(_idFilePath)) File.WriteAllText(_idFilePath, "1");
        
        LoadOfficialServer();

        _chatService.OnMessageReceived += (user, message, color, badge) => {
            Dispatcher.UIThread.Post(() => {
                ChatMessages.Add(new MessageItem { Author = user, Content = message, NameColor = color, Badge = badge });
            });
        };
    }

    private void LoadOfficialServer()
    {
        if (File.Exists(_officialServerPath))
        {
            try {
                var json = File.ReadAllText(_officialServerPath);
                var official = JsonSerializer.Deserialize<ServerItem>(json);
                if (official != null) Servers.Add(official);
            } catch { CreateDefaultServer(); }
        }
        else { CreateDefaultServer(); }
    }

    private void CreateDefaultServer()
    {
        var defaultServer = new ServerItem { Name = "Void Official", Initial = "V" };
        defaultServer.Channels.Add("geral");
        File.WriteAllText(_officialServerPath, JsonSerializer.Serialize(defaultServer));
        Servers.Add(defaultServer);
    }

    // --- COMANDOS DE LOGIN / REGISTRO ---
    [RelayCommand]
    public void ConfirmRegister()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) return;
        string path = Path.Combine(_accountsFolder, $"{UsernameInput.ToLower()}.json");
        if (File.Exists(path)) { TriggerWelcomePopup("Usuário já existe!"); return; }

        string finalNick = string.IsNullOrWhiteSpace(NicknameInput) ? UsernameInput : NicknameInput;
        int nextId = 1;
        int.TryParse(File.ReadAllText(_idFilePath), out nextId);
        nextId++;
        File.WriteAllText(_idFilePath, nextId.ToString());

        var newUser = new UserSettings {
            Nickname = finalNick, Username = UsernameInput.ToLower(), Password = PasswordInput,
            UserId = nextId, Badge = (UsernameInput.ToLower() == "admin") ? "👑" : ""
        };

        File.WriteAllText(path, JsonSerializer.Serialize(newUser));
        TriggerWelcomePopup("Conta Criada!");
        IsRegisterView = false;
    }

    [RelayCommand]
    public void ConfirmLogin()
    {
        string path = Path.Combine(_accountsFolder, $"{UsernameInput.ToLower()}.json");
        if (!File.Exists(path)) { TriggerWelcomePopup("Não encontrado!"); return; }

        var saved = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path));
        if (saved?.Password == PasswordInput) {
            CurrentUser.Nickname = saved.Nickname;
            CurrentUser.Username = saved.Username;
            CurrentUser.UserId = saved.UserId;
            CurrentUser.Badge = saved.Badge;
            IsLoggedIn = true;
            _ = _chatService.ConnectAsync();
            if (Servers.Any()) SelectServer(Servers[0]);
        }
    }

    // --- COMANDOS DE NAVEGAÇÃO ---
    [RelayCommand] public void ToggleView() => IsRegisterView = !IsRegisterView;
    [RelayCommand] public void GoToDms() { IsInServer = false; IsShowingFriends = true; CurrentChatName = "Amigos"; }
    [RelayCommand] public void ToggleCreateServer() => IsCreatingServer = !IsCreatingServer;
    [RelayCommand] public void OpenProfileSettings() { /* Abre config se tiver */ }
    
    [RelayCommand]
    public void AddFriend() { 
        if(!string.IsNullOrWhiteSpace(FriendNameInput)) {
            Friends.Add(new FriendItem { Name = FriendNameInput }); 
            FriendNameInput = ""; 
        }
    }
    
    [RelayCommand]
    public void ConfirmCreateServer() {
        if(!string.IsNullOrWhiteSpace(NewServerName)) {
            var s = new ServerItem { Name = NewServerName, Initial = NewServerName[0].ToString().ToUpper() };
            s.Channels.Add("geral");
            Servers.Add(s);
            IsCreatingServer = false;
            NewServerName = "";
        }
    }

    [RelayCommand]
    public void SelectServer(ServerItem s) {
        if (s == null) return;
        IsInServer = true; IsShowingFriends = false;
        CurrentChannels.Clear();
        foreach(var c in s.Channels) CurrentChannels.Add(c);
        CurrentChatName = s.Channels.FirstOrDefault() ?? "geral";
    }

    [RelayCommand]
    public void ProcessMessage() {
        if(string.IsNullOrWhiteSpace(Input)) return;
        _ = _chatService.SendMessageAsync(CurrentUser.Nickname, Input, CurrentUser.Color, CurrentUser.Badge);
        Input = "";
    }

    private async void TriggerWelcomePopup(string msg) {
        WelcomeMessage = msg; ShowWelcomePopup = true;
        await Task.Delay(3000); ShowWelcomePopup = false;
    }
}