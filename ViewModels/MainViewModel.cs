using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models;
using Void.Services;

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // --- ESTADOS UI ---
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isChatOpen = false;

    // Painel lateral ativo: "addFriend" | "createServer" | ""
    [ObservableProperty] private string _activeSidePanel = "";
    public bool IsAddFriendOpen   => ActiveSidePanel == "addFriend";
    public bool IsCreateServerOpen => ActiveSidePanel == "createServer";

    // --- ÁUDIO ---
    private bool _isMuted = false;
    public bool IsMuted
    {
        get => _isMuted;
        set { SetProperty(ref _isMuted, value); OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor)); }
    }
    private bool _isDeafened = false;
    public bool IsDeafened
    {
        get => _isDeafened;
        set { SetProperty(ref _isDeafened, value); OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor)); }
    }
    public string MuteIcon   => IsMuted    ? "🙊" : "🎤";
    public string DeafenIcon => IsDeafened ? "🔇" : "🎧";
    public string MuteColor  => IsMuted    ? "#F04747" : "#6B7280";
    public string DeafenColor => IsDeafened ? "#F04747" : "#6B7280";

    // --- INPUTS LOGIN ---
    [ObservableProperty] private string _usernameInput = "";
    [ObservableProperty] private string _nicknameInput = "";
    [ObservableProperty] private string _passwordInput = "";
    [ObservableProperty] private string _loginError = "";

    // --- CHAT ---
    [ObservableProperty] private string _input = "";
    [ObservableProperty] private string _currentChatName = "";
    [ObservableProperty] private string _currentChatSubtitle = "";

    // --- ADICIONAR AMIGO ---
    [ObservableProperty] private string _addFriendInput = "";
    [ObservableProperty] private string _addFriendError = "";
    [ObservableProperty] private string _addFriendSuccess = "";

    // --- CRIAR SERVIDOR ---
    [ObservableProperty] private string _newServerName = "";
    [ObservableProperty] private string _newServerError = "";

    // --- USUÁRIO E SERVIÇOS ---
    public UserProfile CurrentUser { get; private set; } = new();
    private readonly AuthenticationService _authService = new();
    private readonly ChatService _chatService = new();
    private string _activePanel = "dm";
    private ServerItem? _selectedServer;
    private FriendItem? _activeDmFriend;
    private ChannelItem? _activeChannel;

    // --- COLEÇÕES ---
    public ObservableCollection<MessageItem> ChatMessages   { get; } = new();
    public ObservableCollection<FriendItem>  Friends        { get; } = new();
    public ObservableCollection<ServerItem>  Servers        { get; } = new();
    public ObservableCollection<ChannelItem> CurrentChannels { get; } = new();

    public string WindowTitle => _activePanel == "server"
        ? $"Void — {_selectedServer?.Name}"
        : "Void — Mensagens";

    partial void OnActiveSidePanelChanged(string value)
    {
        OnPropertyChanged(nameof(IsAddFriendOpen));
        OnPropertyChanged(nameof(IsCreateServerOpen));
    }

    public MainViewModel()
    {
        _chatService.MessageReceived += msg => Dispatcher.UIThread.Post(() =>
        {
            if (msg != null) ChatMessages.Add(msg);
        });
        _chatService.PrivateMessageReceived += msg => Dispatcher.UIThread.Post(() =>
        {
            if (msg != null && _activeDmFriend != null) ChatMessages.Add(msg);
        });
        _chatService.OnUserStatusChanged += (userId, isOnline) => Dispatcher.UIThread.Post(() =>
        {
            var f = Friends.FirstOrDefault(x => x.Name.Equals(userId, StringComparison.OrdinalIgnoreCase));
            if (f != null)
            {
                f.Status = isOnline ? "Online" : "Offline";
                if (_activeDmFriend == f)
                    CurrentChatSubtitle = isOnline ? "● Online" : "○ Offline";
            }
        });
        _chatService.ConnectionFailed += err => Dispatcher.UIThread.Post(() =>
            LoginError = $"Erro de conexão: {err}");
    }

    // ══════════════════════════════════════
    // SESSÃO
    // ══════════════════════════════════════

    [RelayCommand]
    public void ToggleView()
    {
        IsRegisterView = !IsRegisterView;
        UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = "";
    }

    [RelayCommand]
    public async Task ConfirmLogin()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        { LoginError = "Preencha usuário e senha."; return; }
        IsLoading = true; LoginError = "";
        var profile = await _authService.LoginAsync(UsernameInput.Trim(), PasswordInput);
        IsLoading = false;
        if (profile == null) { LoginError = "Usuário ou senha incorretos."; return; }
        await EnterApp(profile);
    }

    [RelayCommand]
    public async Task ConfirmRegister()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        { LoginError = "Preencha usuário e senha."; return; }
        if (PasswordInput.Length < 4) { LoginError = "Senha: mínimo 4 caracteres."; return; }
        IsLoading = true; LoginError = "";
        var profile = await _authService.RegisterAsync(UsernameInput.Trim(), NicknameInput.Trim(), PasswordInput);
        IsLoading = false;
        if (profile == null) { LoginError = "Usuário já existe."; return; }
        await EnterApp(profile);
    }

    private async Task EnterApp(UserProfile profile)
    {
        CurrentUser = profile;
        IsLoggedIn = true;
        OnPropertyChanged(nameof(CurrentUser));
        await _chatService.ConnectAsync(CurrentUser.Username);
        OpenDmPanel();
    }

    [RelayCommand]
    public async Task Logout()
    {
        await _authService.LogoutAsync();
        await _chatService.DisconnectAsync();
        IsLoggedIn = false; IsChatOpen = false;
        ChatMessages.Clear(); Friends.Clear(); CurrentChannels.Clear(); Servers.Clear();
        UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = "";
        ActiveSidePanel = "";
    }

    // ══════════════════════════════════════
    // NAVEGAÇÃO
    // ══════════════════════════════════════

    [RelayCommand]
    public void OpenDmPanel()
    {
        _activePanel = "dm";
        IsInServer = false;
        _selectedServer = null;
        _activeChannel = null;
        IsChatOpen = _activeDmFriend != null;
        ActiveSidePanel = "";
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    public async Task SelectServer(ServerItem s)
    {
        if (s == null) return;
        _activePanel = "server";
        _selectedServer = s;
        IsInServer = true;
        _activeDmFriend = null;
        ActiveSidePanel = "";
        CurrentChannels.Clear();
        if (s.Channels != null)
            foreach (var c in s.Channels)
                CurrentChannels.Add(c);
        var first = CurrentChannels.FirstOrDefault(c => c.Type == ChannelType.Text);
        if (first != null) await SelectChannel(first);
        else IsChatOpen = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    // ══════════════════════════════════════
    // CRIAR SERVIDOR
    // ══════════════════════════════════════

    [RelayCommand]
    public void ToggleCreateServer()
    {
        ActiveSidePanel = IsCreateServerOpen ? "" : "createServer";
        NewServerName = ""; NewServerError = "";
    }

    [RelayCommand]
    public void ConfirmCreateServer()
    {
        var name = NewServerName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        { NewServerError = "Digite um nome para o servidor."; return; }
        if (name.Length < 2)
        { NewServerError = "Nome muito curto (mín. 2 caracteres)."; return; }
        if (Servers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { NewServerError = "Já existe um servidor com esse nome."; return; }

        var server = new ServerItem
        {
            Id = Servers.Count + 1,
            Name = name,
            OwnerId = CurrentUser.Id,
            Channels = new System.Collections.Generic.List<ChannelItem>
            {
                new ChannelItem { Id = 1, Name = "geral", Type = ChannelType.Text, Topic = "Canal principal" },
                new ChannelItem { Id = 2, Name = "off-topic", Type = ChannelType.Text, Topic = "Assuntos livres" },
            }
        };

        Servers.Add(server);
        NewServerName = ""; NewServerError = ""; ActiveSidePanel = "";

        // Entra automaticamente no servidor criado
        _ = SelectServer(server);
    }

    // ══════════════════════════════════════
    // CANAIS
    // ══════════════════════════════════════

    [RelayCommand]
    public async Task SelectChannel(ChannelItem c)
    {
        if (c == null) return;
        _activeChannel = c;
        _activeDmFriend = null;
        CurrentChatName = c.Name;
        CurrentChatSubtitle = c.Topic ?? "Canal de texto";
        IsChatOpen = true;
        ChatMessages.Clear();
        if (_chatService.IsConnected && _selectedServer != null)
            await _chatService.JoinChannelAsync(_selectedServer.Name, c.Name);
    }

    // ══════════════════════════════════════
    // AMIGOS
    // ══════════════════════════════════════

    [RelayCommand]
    public void ToggleAddFriend()
    {
        ActiveSidePanel = IsAddFriendOpen ? "" : "addFriend";
        AddFriendInput = ""; AddFriendError = ""; AddFriendSuccess = "";
    }

    [RelayCommand]
    public void ConfirmAddFriend()
    {
        var name = AddFriendInput.Trim();
        AddFriendError = ""; AddFriendSuccess = "";
        if (string.IsNullOrWhiteSpace(name))
        { AddFriendError = "Digite um nome de usuário."; return; }
        if (name.Equals(CurrentUser.Username, StringComparison.OrdinalIgnoreCase))
        { AddFriendError = "Você não pode adicionar a si mesmo."; return; }
        if (Friends.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { AddFriendError = "Já está na sua lista."; return; }

        Friends.Add(new FriendItem
        {
            Name = name,
            Nickname = name,
            Status = "Offline",
            Initials = name[0].ToString().ToUpper(),
            AvatarColor = GetAvatarColor(name)
        });

        AddFriendSuccess = $"✓ {name} adicionado!";
        AddFriendInput = "";

        // Fecha o painel após 1.5s
        DispatcherTimer.RunOnce(() =>
        {
            AddFriendSuccess = "";
            ActiveSidePanel = "";
        }, TimeSpan.FromSeconds(1.5));
    }

    [RelayCommand]
    public void RemoveFriend(FriendItem f)
    {
        if (f == null) return;
        if (_activeDmFriend == f) { _activeDmFriend = null; IsChatOpen = false; }
        Friends.Remove(f);
    }

    [RelayCommand]
    public void SelectFriend(FriendItem f)
    {
        if (f == null) return;
        _activeDmFriend = f;
        _activeChannel = null;
        _activePanel = "dm";
        IsInServer = false;
        CurrentChatName = f.Nickname.Length > 0 ? f.Nickname : f.Name;
        CurrentChatSubtitle = f.IsOnline ? "● Online" : "○ Offline";
        IsChatOpen = true;
        ChatMessages.Clear();
        ActiveSidePanel = "";
        OnPropertyChanged(nameof(WindowTitle));
    }

    // ══════════════════════════════════════
    // MENSAGEM
    // ══════════════════════════════════════

    [RelayCommand]
    public async Task ProcessMessage()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        var msg = new MessageItem { Content = Input, Author = CurrentUser, Timestamp = DateTime.Now };
        Input = "";
        if (_activeChannel != null && _chatService.IsConnected && _selectedServer != null)
            await _chatService.SendMessageAsync(msg, _selectedServer.Name, _activeChannel.Name);
        else if (_activeDmFriend != null && _chatService.IsConnected)
        { ChatMessages.Add(msg); await _chatService.SendPrivateMessageAsync(_activeDmFriend.Name, msg.Content, CurrentUser.Username); }
        else
            _chatService.SimulateMessageReceived(msg);
    }

    // ══════════════════════════════════════
    // ÁUDIO
    // ══════════════════════════════════════

    [RelayCommand] public void ToggleMute()   => IsMuted    = !IsMuted;
    [RelayCommand] public void ToggleDeafen() { IsDeafened  = !IsDeafened; IsMuted = IsDeafened; }
    [RelayCommand] public void OpenSettings() { }

    private static string GetAvatarColor(string name)
    {
        var colors = new[] { "#5865F2", "#57F287", "#FEE75C", "#EB459E", "#ED4245", "#00C9A7", "#8B5CF6" };
        return colors[Math.Abs(name.GetHashCode()) % colors.Length];
    }
}