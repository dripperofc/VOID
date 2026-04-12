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
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isChatOpen = false;
    [ObservableProperty] private string _activeSidePanel = "";
    [ObservableProperty] private string _dmTab = "conversations";

    public bool IsAddFriendOpen      => ActiveSidePanel == "addFriend";
    public bool IsCreateServerOpen   => ActiveSidePanel == "createServer";
    public bool IsConversationsTab   => DmTab == "conversations";
    public bool IsFriendRequestsTab  => DmTab == "requests";

    partial void OnDmTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsConversationsTab));
        OnPropertyChanged(nameof(IsFriendRequestsTab));
        OnPropertyChanged(nameof(PendingRequestsCount));
        OnPropertyChanged(nameof(HasPendingRequests));
    }

    partial void OnActiveSidePanelChanged(string value)
    {
        OnPropertyChanged(nameof(IsAddFriendOpen));
        OnPropertyChanged(nameof(IsCreateServerOpen));
    }

    private bool _isMuted = false;
    public bool IsMuted
    {
        get => _isMuted;
        set { SetProperty(ref _isMuted, value); OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor)); SoundService.Muted = value; }
    }

    private bool _isDeafened = false;
    public bool IsDeafened
    {
        get => _isDeafened;
        set { SetProperty(ref _isDeafened, value); OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor)); }
    }

    public string MuteIcon    => IsMuted    ? "X" : "M";
    public string DeafenIcon  => IsDeafened ? "X" : "H";
    public string MuteColor   => IsMuted    ? "#F04747" : "#6B7280";
    public string DeafenColor => IsDeafened ? "#F04747" : "#6B7280";

    [ObservableProperty] private string _usernameInput = "";
    [ObservableProperty] private string _nicknameInput = "";
    [ObservableProperty] private string _passwordInput = "";
    [ObservableProperty] private string _loginError = "";
    [ObservableProperty] private string _input = "";
    [ObservableProperty] private string _currentChatName = "";
    [ObservableProperty] private string _currentChatSubtitle = "";
    [ObservableProperty] private string _addFriendInput = "";
    [ObservableProperty] private string _addFriendError = "";
    [ObservableProperty] private string _addFriendSuccess = "";
    [ObservableProperty] private string _newServerName = "";
    [ObservableProperty] private string _newServerError = "";

    public UserProfile CurrentUser { get; private set; } = new();
    private readonly AuthenticationService _authService = new();
    private readonly ChatService _chatService = new();
    private string _activePanel = "dm";
    private ServerItem? _selectedServer;
    private FriendItem? _activeDmFriend;
    private ChannelItem? _activeChannel;
    private DispatcherTimer? _closePanelTimer;

    public ObservableCollection<MessageItem> ChatMessages   { get; } = new();
    public ObservableCollection<FriendItem>  Friends        { get; } = new();
    public ObservableCollection<FriendItem>  FriendRequests { get; } = new();
    public ObservableCollection<ServerItem>  Servers        { get; } = new();
    public ObservableCollection<ChannelItem> CurrentChannels { get; } = new();

    public int  PendingRequestsCount => FriendRequests.Count;
    public bool HasPendingRequests   => FriendRequests.Count > 0;

    public string WindowTitle => _activePanel == "server"
        ? $"Void - {_selectedServer?.Name}"
        : "Void - Mensagens";

    public MainViewModel()
    {
        FriendRequests.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PendingRequestsCount));
            OnPropertyChanged(nameof(HasPendingRequests));
        };

        _chatService.MessageReceived += msg => Dispatcher.UIThread.Post(() =>
        {
            if (msg == null) return;
            ChatMessages.Add(msg);
            if (msg.Author?.Username != CurrentUser.Username) SoundService.Play("message");
        });

        _chatService.PrivateMessageReceived += msg => Dispatcher.UIThread.Post(() =>
        {
            if (msg == null) return;
            if (msg.Author?.Username == CurrentUser.Username) return; // ignora echo proprio
            if (_activeDmFriend == null) return;
            ChatMessages.Add(msg);
            SoundService.Play("message");
        });

        _chatService.OnUserStatusChanged += (userId, isOnline) => Dispatcher.UIThread.Post(() =>
        {
            var f = Friends.FirstOrDefault(x => x.Name.Equals(userId, StringComparison.OrdinalIgnoreCase));
            if (f == null) return;
            f.Status = isOnline ? "Online" : "Offline";
            if (_activeDmFriend == f)
                CurrentChatSubtitle = isOnline ? "Online" : "Offline"; // FIX: atualiza header
            SoundService.Play(isOnline ? "join" : "disconnect");
        });

        _chatService.ConnectionFailed += err => Dispatcher.UIThread.Post(() =>
            LoginError = $"Erro de conexao: {err}");
    }

    [RelayCommand] public void ToggleView() { IsRegisterView = !IsRegisterView; UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = ""; }

    [RelayCommand]
    public async Task ConfirmLogin()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) { LoginError = "Preencha usuario e senha."; return; }
        IsLoading = true; LoginError = "";
        var profile = await _authService.LoginAsync(UsernameInput.Trim(), PasswordInput);
        IsLoading = false;
        if (profile == null) { LoginError = "Usuario ou senha incorretos."; return; }
        await EnterApp(profile);
    }

    [RelayCommand]
    public async Task ConfirmRegister()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) { LoginError = "Preencha usuario e senha."; return; }
        if (PasswordInput.Length < 4) { LoginError = "Senha: minimo 4 caracteres."; return; }
        IsLoading = true; LoginError = "";
        var profile = await _authService.RegisterAsync(UsernameInput.Trim(), NicknameInput.Trim(), PasswordInput);
        IsLoading = false;
        if (profile == null) { LoginError = "Usuario ja existe."; return; }
        await EnterApp(profile);
    }

    private async Task EnterApp(UserProfile profile)
    {
        CurrentUser = profile; IsLoggedIn = true;
        OnPropertyChanged(nameof(CurrentUser));
        await _chatService.ConnectAsync(CurrentUser.Username);
        OpenDmPanel();
    }

    [RelayCommand]
    public async Task Logout()
    {
        _closePanelTimer?.Stop();
        await _authService.LogoutAsync();
        await _chatService.DisconnectAsync();
        IsLoggedIn = false; IsChatOpen = false; DmTab = "conversations";
        ChatMessages.Clear(); Friends.Clear(); FriendRequests.Clear();
        CurrentChannels.Clear(); Servers.Clear();
        UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = ""; ActiveSidePanel = "";
    }

    [RelayCommand]
    public void OpenDmPanel()
    {
        _activePanel = "dm"; IsInServer = false; _selectedServer = null; _activeChannel = null;
        IsChatOpen = _activeDmFriend != null; ActiveSidePanel = "";
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    public async Task SelectServer(ServerItem s)
    {
        if (s == null) return;
        _activePanel = "server"; _selectedServer = s; IsInServer = true; _activeDmFriend = null; ActiveSidePanel = "";
        CurrentChannels.Clear();
        if (s.Channels != null) foreach (var c in s.Channels) CurrentChannels.Add(c);
        var first = CurrentChannels.FirstOrDefault(c => c.Type == ChannelType.Text);
        if (first != null) await SelectChannel(first); else IsChatOpen = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand] public void ShowConversations() => DmTab = "conversations";
    [RelayCommand] public void ShowFriendRequests() => DmTab = "requests";

    [RelayCommand] public void ToggleCreateServer() { ActiveSidePanel = IsCreateServerOpen ? "" : "createServer"; NewServerName = ""; NewServerError = ""; }

    [RelayCommand]
    public async Task ConfirmCreateServer()
    {
        var name = NewServerName.Trim();
        if (string.IsNullOrWhiteSpace(name)) { NewServerError = "Digite um nome."; return; }
        if (name.Length < 2) { NewServerError = "Nome muito curto."; return; }
        if (Servers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) { NewServerError = "Nome ja existe."; return; }

        var server = new ServerItem
        {
            Id = Servers.Count + 1, Name = name, OwnerId = CurrentUser.Id,
            Channels = new System.Collections.Generic.List<ChannelItem>
            {
                new ChannelItem { Id = 1, Name = "geral",     Type = ChannelType.Text, Topic = "Canal principal" },
                new ChannelItem { Id = 2, Name = "off-topic", Type = ChannelType.Text, Topic = "Assuntos livres" },
            }
        };
        Servers.Add(server);
        NewServerName = ""; NewServerError = ""; ActiveSidePanel = "";
        await SelectServer(server);
    }

    [RelayCommand]
    public async Task SelectChannel(ChannelItem c)
    {
        if (c == null) return;
        _activeChannel = c; _activeDmFriend = null;
        CurrentChatName = c.Name; CurrentChatSubtitle = c.Topic ?? "Canal de texto";
        IsChatOpen = true; ChatMessages.Clear();
        if (_chatService.IsConnected && _selectedServer != null)
            await _chatService.JoinChannelAsync(_selectedServer.Name, c.Name);
    }

    [RelayCommand] public void ToggleAddFriend() { ActiveSidePanel = IsAddFriendOpen ? "" : "addFriend"; AddFriendInput = ""; AddFriendError = ""; AddFriendSuccess = ""; }

    [RelayCommand]
    public void ConfirmAddFriend()
    {
        var name = AddFriendInput.Trim();
        AddFriendError = ""; AddFriendSuccess = "";
        if (string.IsNullOrWhiteSpace(name)) { AddFriendError = "Digite um nome de usuario."; return; }
        if (name.Equals(CurrentUser.Username, StringComparison.OrdinalIgnoreCase)) { AddFriendError = "Voce nao pode adicionar a si mesmo."; return; }
        if (Friends.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) { AddFriendError = "Ja esta na sua lista."; return; }

        Friends.Add(new FriendItem { Name = name, Nickname = name, Status = "Offline", Initials = name[0].ToString().ToUpper(), AvatarColor = GetAvatarColor(name) });
        AddFriendSuccess = $"+ {name} adicionado!";
        AddFriendInput = "";
        SoundService.Play("join");

        _closePanelTimer?.Stop();
        _closePanelTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _closePanelTimer.Tick += (_, _) => { _closePanelTimer!.Stop(); AddFriendSuccess = ""; ActiveSidePanel = ""; };
        _closePanelTimer.Start();
    }

    [RelayCommand]
    public void AcceptFriendRequest(FriendItem f)
    {
        if (f == null) return;
        FriendRequests.Remove(f);
        f.Status = "Online";
        Friends.Add(f);
        SoundService.Play("join");
    }

    [RelayCommand]
    public void DeclineFriendRequest(FriendItem f)
    {
        if (f == null) return;
        FriendRequests.Remove(f);
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
        _activeDmFriend = f; _activeChannel = null; _activePanel = "dm"; IsInServer = false;
        CurrentChatName = f.Nickname.Length > 0 ? f.Nickname : f.Name;
        CurrentChatSubtitle = f.IsOnline ? "Online" : "Offline"; // FIX: usa status real
        IsChatOpen = true; ChatMessages.Clear(); ActiveSidePanel = "";
        OnPropertyChanged(nameof(WindowTitle));
    }

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

    [RelayCommand] public void ToggleMute()   { IsMuted = !IsMuted; SoundService.Play(IsMuted ? "pttoff" : "ptton"); }
    [RelayCommand] public void ToggleDeafen() { IsDeafened = !IsDeafened; IsMuted = IsDeafened; SoundService.Play(IsDeafened ? "deafen" : "undeafen"); }
    [RelayCommand] public void OpenSettings() { }

    private static string GetAvatarColor(string name)
    {
        var colors = new[] { "#5865F2", "#57F287", "#FEE75C", "#EB459E", "#ED4245", "#00C9A7", "#8B5CF6" };
        return colors[Math.Abs(name.GetHashCode()) % colors.Length];
    }
}