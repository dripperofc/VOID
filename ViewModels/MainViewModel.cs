using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models; 
using Void.Services;

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _showWelcomePopup = false;
    [ObservableProperty] private bool _isShowingFriends = true;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private string _usernameInput = "";
    [ObservableProperty] private string _nicknameInput = "";
    [ObservableProperty] private string _passwordInput = "";
    [ObservableProperty] private string _welcomeMessage = "";
    [ObservableProperty] private string _input = "";
    [ObservableProperty] private string _currentChatName = "Amigos";

    public UserProfile CurrentUser { get; } = new();
    private readonly ChatService _chatService = new(); 
    private readonly string _accountsFolder = "Accounts";
    private readonly string _idFilePath = "last_id.txt";

    public ObservableCollection<MessageItem> ChatMessages { get; } = new();
    public ObservableCollection<FriendItem> Friends { get; } = new(); 
    public ObservableCollection<ServerItem> Servers { get; } = new();

    public MainViewModel()
    {
        if (!Directory.Exists(_accountsFolder)) Directory.CreateDirectory(_accountsFolder);
        if (!File.Exists(_idFilePath)) File.WriteAllText(_idFilePath, "1");

        _chatService.OnMessageReceived += (user, message, color, badge) => {
            Dispatcher.UIThread.Post(() => {
                // Aqui o C# usa as propriedades "Pai" geradas pelo seu [ObservableProperty]
                ChatMessages.Add(new MessageItem { 
                    Author = user, 
                    Content = message, 
                    NameColor = color, 
                    Badge = badge 
                    // O Timestamp já inicia com DateTime.Now no seu Model!
                });
            });
        };
    }

    private int GetNextUserId(string username)
    {
        if (username.ToLower() == "admin" || username.ToLower() == "dono") return 1;
        int lastId = 1;
        if (File.Exists(_idFilePath)) int.TryParse(File.ReadAllText(_idFilePath), out lastId);
        int nextId = lastId + 1;
        File.WriteAllText(_idFilePath, nextId.ToString());
        return nextId;
    }

    [RelayCommand]
    public void ConfirmRegister()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) return;
        string path = Path.Combine(_accountsFolder, $"{UsernameInput.ToLower()}.json");
        if (File.Exists(path)) { TriggerWelcomePopup("Usuário já existe!"); return; }

        string finalNick = string.IsNullOrWhiteSpace(NicknameInput) ? UsernameInput : NicknameInput;
        int idNovo = GetNextUserId(UsernameInput);

        var newUser = new UserSettings {
            Nickname = finalNick, Username = UsernameInput.ToLower(), Password = PasswordInput,
            UserId = idNovo, Badge = (idNovo == 1) ? "👑" : ""
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
        } else { TriggerWelcomePopup("Senha errada!"); }
    }

    [RelayCommand] public void ToggleView() => IsRegisterView = !IsRegisterView;
    [RelayCommand] public void GoToDms() { IsInServer = false; IsShowingFriends = true; CurrentChatName = "Amigos"; }
    
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