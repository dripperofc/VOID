using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models; 
using Void.Services;
using NAudio.Wave;

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // --- ESTADOS DA UI ---
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _showWelcomePopup = false;
    [ObservableProperty] private bool _isShowingFriends = true;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isCreatingServer = false;

    // ==========================================
    // ESTADOS DE ÁUDIO (ESCRITOS NA MÃO PARA NÃO DAR ERRO)
    // ==========================================
    private bool _isMuted = false;
    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    private bool _isDeafened = false;
    public bool IsDeafened
    {
        get => _isDeafened;
        set => SetProperty(ref _isDeafened, value);
    }

    // --- ÍCONES DINÂMICOS ---
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
    [ObservableProperty] private string _currentChatName = "Bem-vindo às DMs";
    [ObservableProperty] private string _friendNameInput = "";
    [ObservableProperty] private string _newServerName = "";

    // --- CONTROLE DE ROTAS ---
    private string _currentServerId = "global";
    private string _currentChannelId = "geral";

    public UserProfile CurrentUser { get; } = new();
    private readonly ChatService _chatService = new(); 
    
    // --- CAMINHOS ---
    private readonly string _baseFolder;
    private readonly string _accountsFolder;
    private readonly string _idFilePath;
    private readonly string _officialServerPath;

    // --- LISTAS ---
    public ObservableCollection<MessageItem> ChatMessages { get; } = new();
    public ObservableCollection<MessageItem> PrivateMessages { get; } = new();
    public ObservableCollection<FriendItem> Friends { get; } = new(); 
    public ObservableCollection<ServerItem> Servers { get; } = new();
    public ObservableCollection<string> CurrentChannels { get; } = new();

    public string WindowTitle
    {
        get
        {
            if (!IsLoggedIn) return IsRegisterView ? "Void - Criar Conta" : "Void - Login";
            if (IsInServer) return $"Void - {_currentServerId} | #{_currentChannelId}";
            return "Void - @me (Minhas DMs)";
        }
    }

    public MainViewModel()
    {
        _baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Void");
        _accountsFolder = Path.Combine(_baseFolder, "Accounts");
        _idFilePath = Path.Combine(_baseFolder, "last_id.txt");
        _officialServerPath = Path.Combine(_baseFolder, "official_server.json");

        if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
        if (!Directory.Exists(_accountsFolder)) Directory.CreateDirectory(_accountsFolder);
        if (!File.Exists(_idFilePath)) File.WriteAllText(_idFilePath, "1");
        
        LoadOfficialServer();

        // ==========================================
        // RECEBENDO MENSAGENS 
        // ==========================================
        _chatService.OnMessageReceived += (msg) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (IsInServer)
                {
                    ChatMessages.Add(new MessageItem
                    {
                        Author = msg.Username,
                        Content = msg.Message,
                        NameColor = msg.Color,
                        Badge = msg.Badge,
                        Timestamp = msg.Timestamp.HasValue ? msg.Timestamp.Value.ToLocalTime().ToString("HH:mm") : DateTime.Now.ToString("HH:mm")
                    });
                }
            });
        };

        _chatService.OnHistoryLoaded += (messages) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                ChatMessages.Clear();
                foreach (var msg in messages)
                {
                    ChatMessages.Add(new MessageItem
                    {
                        Author = msg.Username,
                        Content = msg.Message,
                        NameColor = msg.Color,
                        Badge = msg.Badge,
                        Timestamp = msg.Timestamp.HasValue ? msg.Timestamp.Value.ToLocalTime().ToString("HH:mm") : DateTime.Now.ToString("HH:mm")
                    });
                }
            });
        };

        _chatService.OnPrivateMessageReceived += (msg) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsInServer)
                {
                    ChatMessages.Add(new MessageItem
                    {
                        Author = msg.Username,
                        Content = msg.Message,
                        NameColor = msg.Color,
                        Timestamp = msg.Timestamp.HasValue ? msg.Timestamp.Value.ToLocalTime().ToString("HH:mm") : DateTime.Now.ToString("HH:mm")
                    });
                }
            });
        };
    }

    private void LoadOfficialServer()
    {
        if (File.Exists(_officialServerPath))
        {
            var json = File.ReadAllText(_officialServerPath);
            var official = JsonSerializer.Deserialize<ServerItem>(json);
            if (official != null) Servers.Add(official);
        }
        else
        {
            var defaultServer = new ServerItem { Name = "Void Official", Initial = "V" };
            defaultServer.Channels.Add("geral");
            File.WriteAllText(_officialServerPath, JsonSerializer.Serialize(defaultServer));
            Servers.Add(defaultServer);
        }
    }

    // ==========================================
    // SISTEMA DE ÁUDIO (.MP3 COM NAUDIO)
    // ==========================================
    private void PlaySound(string fileName)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", fileName);
        if (File.Exists(path))
        {
            Task.Run(() =>
            {
                try
                {
                    using var audioFile = new AudioFileReader(path);
                    using var outputDevice = new WaveOutEvent();
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch { }
            });
        }
    }

    [RelayCommand]
    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        
        if (IsMuted) PlaySound("mute.mp3"); else PlaySound("unmute.mp3");
        if (!IsMuted && IsDeafened) { IsDeafened = false; PlaySound("undeafen.mp3"); }
        
        OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor));
        OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor));
    }

    [RelayCommand]
    public void ToggleDeafen()
    {
        IsDeafened = !IsDeafened;
        
        if (IsDeafened) { IsMuted = true; PlaySound("deafen.mp3"); } 
        else { PlaySound("undeafen.mp3"); }
        
        OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor));
        OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor));
    }

    [RelayCommand]
    public void OpenSettings() => TriggerWelcomePopup("Configurações do Void abertas!");

    // ==========================================
    // AUTENTICAÇÃO E ROTEAMENTO
    // ==========================================
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

        var newUser = new UserSettings
        {
            Nickname = finalNick, Username = UsernameInput.ToLower(),
            Password = HashPassword(PasswordInput), UserId = nextId,
            Badge = (UsernameInput.ToLower() == "admin" || UsernameInput.ToLower() == "dono") ? "👑" : ""
        };

        File.WriteAllText(path, JsonSerializer.Serialize(newUser));

        TriggerWelcomePopup("Conta Criada!");
        IsRegisterView = false; PasswordInput = "";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    [RelayCommand]
    public void ConfirmLogin()
    {
        string path = Path.Combine(_accountsFolder, $"{UsernameInput.ToLower()}.json");
        if (!File.Exists(path)) { TriggerWelcomePopup("Não encontrado!"); return; }

        var saved = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path));

        if (saved?.Password == HashPassword(PasswordInput))
        {
            CurrentUser.Nickname = saved.Nickname; CurrentUser.Username = saved.Username;
            CurrentUser.UserId = saved.UserId; CurrentUser.Badge = saved.Badge;

            IsLoggedIn = true;
            _ = _chatService.ConnectAsync(CurrentUser.UserId.ToString());

            GoToDms();
            OnPropertyChanged(nameof(WindowTitle));
        }
        else TriggerWelcomePopup("Senha incorreta!");

        PasswordInput = "";
    }

    [RelayCommand] public void ToggleView() { IsRegisterView = !IsRegisterView; OnPropertyChanged(nameof(WindowTitle)); }
    [RelayCommand] public void ToggleCreateServer() => IsCreatingServer = !IsCreatingServer;

    [RelayCommand] 
    public void GoToDms() 
    { 
        IsInServer = false; IsShowingFriends = true; 
        CurrentChatName = "Bem-vindo às DMs"; ChatMessages.Clear();
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    public void AddFriend() 
    { 
        if(!string.IsNullOrWhiteSpace(FriendNameInput)) 
        {
            Friends.Add(new FriendItem { Name = FriendNameInput }); 
            FriendNameInput = ""; 
        }
    }

    [RelayCommand]
    public void SelectFriend(FriendItem f)
    {
        if (f == null) return;
        CurrentChatName = f.Name; ChatMessages.Clear(); 
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    public void SelectServer(ServerItem s)
    {
        if (s == null) return;
        IsInServer = true; IsShowingFriends = false;
        _currentServerId = s.Name.ToLower().Replace(" ", "_");

        CurrentChannels.Clear();
        foreach (var c in s.Channels) CurrentChannels.Add(c);

        SelectChannel(CurrentChannels.FirstOrDefault() ?? "geral");
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    public void SelectChannel(string channel)
    {
        _currentChannelId = channel; CurrentChatName = channel;
        _ = _chatService.JoinChannelAsync(_currentServerId, _currentChannelId);
        OnPropertyChanged(nameof(WindowTitle));
    }

    // ==========================================
    // ENVIO DE MENSAGENS
    // ==========================================
    [RelayCommand]
    public async Task ProcessMessage() 
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        string tempInput = Input; Input = ""; 

        try
        {
            if (IsInServer)
            {
                await _chatService.SendMessageAsync(
                    CurrentUser.Nickname, tempInput,
                    string.IsNullOrWhiteSpace(CurrentUser.Color) ? "#FFFFFF" : CurrentUser.Color,
                    CurrentUser.Badge ?? "", _currentServerId, _currentChannelId
                );
            }
            else
            {
                if (CurrentChatName.Contains("Bem-vindo")) 
                {
                    TriggerWelcomePopup("Selecione ou adicione um amigo primeiro!");
                    Input = tempInput; return;
                }

                await _chatService.SendPrivateMessageAsync(CurrentChatName, tempInput, CurrentUser.Nickname);
                ChatMessages.Add(new MessageItem {
                    Author = CurrentUser.Nickname, Content = tempInput,
                    NameColor = string.IsNullOrWhiteSpace(CurrentUser.Color) ? "#00D4AA" : CurrentUser.Color,
                    Timestamp = DateTime.Now.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            Input = tempInput; TriggerWelcomePopup("Erro de conexão com o VoidServer!");
            Console.WriteLine(ex.Message);
        }
    }

    private void TriggerWelcomePopup(string msg) => _ = TriggerWelcomePopupAsync(msg);
    private async Task TriggerWelcomePopupAsync(string msg) { WelcomeMessage = msg; ShowWelcomePopup = true; await Task.Delay(3000); ShowWelcomePopup = false; }
}