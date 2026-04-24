using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models;
using Void.Services;

namespace Void.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // UI state
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isChatOpen = false;
    [ObservableProperty] private bool _isAddFriendOpen = false;
    [ObservableProperty] private bool _isCreateServerOpen = false;

    // Aba DMs: "conversations" | "requests"
    [ObservableProperty] private string _dmTab = "conversations";
    public bool IsConversationsTab  => DmTab == "conversations";
    public bool IsFriendRequestsTab => DmTab == "requests";
    public int  PendingCount        => PendingRequests.Count;
    public bool HasPending          => PendingRequests.Count > 0;
    partial void OnDmTabChanged(string value) { OnPropertyChanged(nameof(IsConversationsTab)); OnPropertyChanged(nameof(IsFriendRequestsTab)); }

    // Áudio
    private bool _isMuted = false;
    public bool IsMuted { get => _isMuted; set { SetProperty(ref _isMuted, value); OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor)); SoundService.Muted = value; } }
    private bool _isDeafened = false;
    public bool IsDeafened { get => _isDeafened; set { SetProperty(ref _isDeafened, value); OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor)); } }
    public string MuteIcon    => IsMuted    ? "X" : "M";
    public string DeafenIcon  => IsDeafened ? "X" : "H";
    public string MuteColor   => IsMuted    ? "#F04747" : "#6B7280";
    public string DeafenColor => IsDeafened ? "#F04747" : "#6B7280";

    // Inputs
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

    // Dados
    public UserProfile CurrentUser { get; private set; } = new();
    private readonly ChatService _chatService = new();
    private FriendItem? _activeDmFriend;
    private ChannelItem? _activeChannel;
    private ServerItem? _selectedServer;
    private DispatcherTimer? _timer;

    public ObservableCollection<MessageItem> ChatMessages    { get; } = new();
    public ObservableCollection<FriendItem>  Friends         { get; } = new();
    public ObservableCollection<FriendItem>  PendingRequests { get; } = new();
    public ObservableCollection<ServerItem>  Servers         { get; } = new();
    public ObservableCollection<ChannelItem> CurrentChannels { get; } = new();

    public string WindowTitle => IsInServer ? $"Void - {_selectedServer?.Name}" : "Void - Mensagens";

    public MainViewModel()
    {
        PendingRequests.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(PendingCount)); OnPropertyChanged(nameof(HasPending)); };

        _chatService.MessageReceived += msg => Dispatcher.UIThread.Post(() =>
        {
            if (msg == null) return;
            ChatMessages.Add(msg);
            if (msg.Author?.Username != CurrentUser.Username) SoundService.Play("message");
        });

        _chatService.PrivateMessageReceived += msg => Dispatcher.UIThread.Post(() =>
        {
            if (msg == null) return;
            // ignora echo das proprias mensagens
            if (msg.Author?.Nickname == CurrentUser.Nickname || msg.Author?.Username == CurrentUser.Username) return;
            if (_activeDmFriend == null) return;
            ChatMessages.Add(msg);
            SoundService.Play("message");
        });

        _chatService.OnUserStatusChanged += (userId, isOnline) => Dispatcher.UIThread.Post(() =>
        {
            var f = Friends.FirstOrDefault(x => x.Name.Equals(userId, StringComparison.OrdinalIgnoreCase));
            if (f == null) return;
            f.Status = isOnline ? "Online" : "Offline";
            if (_activeDmFriend == f) CurrentChatSubtitle = isOnline ? "Online" : "Offline";
            SoundService.Play(isOnline ? "join" : "disconnect");
        });

        _chatService.FriendRequestReceived += from => Dispatcher.UIThread.Post(() =>
        {
            if (PendingRequests.Any(p => p.Name.Equals(from, StringComparison.OrdinalIgnoreCase))) return;
            PendingRequests.Add(new FriendItem { Name = from, Nickname = from, Status = "Online", Initials = from[0].ToString().ToUpper(), AvatarColor = AvatarColor(from) });
            SoundService.Play("message");
        });

        _chatService.FriendRequestFailed += reason => Dispatcher.UIThread.Post(() =>
            AddFriendError = reason);

        _chatService.FriendRequestSent += to => Dispatcher.UIThread.Post(() =>
        {
            AddFriendSuccess = $"Pedido enviado para {to}!";
            AddFriendInput = "";
            StartCloseTimer();
        });

        _chatService.FriendAccepted += friend => Dispatcher.UIThread.Post(() =>
        {
            AddFriendToList(friend);
            SoundService.Play("join");
        });

        _chatService.ConnectionFailed += err => Dispatcher.UIThread.Post(() =>
            LoginError = $"Servidor offline: {err}");
    }

    // SESSAO
    [RelayCommand] public void ToggleView() { IsRegisterView = !IsRegisterView; UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = ""; }

    [RelayCommand]
    public async Task ConfirmLogin()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) { LoginError = "Preencha usuario e senha."; return; }
        IsLoading = true; LoginError = "";
        var result = await _chatService.AuthenticateAsync(UsernameInput.Trim().ToLower(), PasswordInput, false);
        IsLoading = false;
        if (result == "ok") await EnterApp(UsernameInput.Trim().ToLower());
        else if (result == "invalid_credentials") LoginError = "Usuario ou senha incorretos.";
        else LoginError = "Servidor offline ou erro desconhecido.";
    }

    [RelayCommand]
    public async Task ConfirmRegister()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) { LoginError = "Preencha usuario e senha."; return; }
        if (PasswordInput.Length < 4) { LoginError = "Senha: minimo 4 caracteres."; return; }
        IsLoading = true; LoginError = "";
        var result = await _chatService.AuthenticateAsync(UsernameInput.Trim().ToLower(), PasswordInput, true);
        IsLoading = false;
        if (result == "ok") await EnterApp(UsernameInput.Trim().ToLower());
        else if (result == "user_exists") LoginError = "Usuario ja existe.";
        else LoginError = "Servidor offline. Nao e possivel criar conta.";
    }

    private async Task EnterApp(string username)
    {
        // Busca perfil completo do servidor
        var profile = await _chatService.GetUserProfileAsync(username);
        CurrentUser = new UserProfile
        {
            Username  = username,
            Nickname  = profile?.Nickname ?? username,
            AvatarColor = profile?.AvatarColor ?? "#5865F2",
            Initials  = profile?.Initials ?? username[0].ToString().ToUpper()
        };
        OnPropertyChanged(nameof(CurrentUser));

        // Carrega lista de amigos do perfil
        if (profile?.Friends != null)
            foreach (var f in profile.Friends)
                AddFriendToList(f);

        // Carrega pedidos pendentes
        var pending = await _chatService.GetPendingRequestsAsync(username);
        foreach (var p in pending)
            if (!PendingRequests.Any(x => x.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
                PendingRequests.Add(new FriendItem { Name = p, Nickname = p, Status = "Online", Initials = p[0].ToString().ToUpper(), AvatarColor = AvatarColor(p) });

        IsLoggedIn = true;
        OpenDmPanel();
    }

    [RelayCommand]
    public async Task Logout()
    {
        _timer?.Stop();
        await _chatService.DisconnectAsync();
        IsLoggedIn = false; IsChatOpen = false; DmTab = "conversations";
        ChatMessages.Clear(); Friends.Clear(); PendingRequests.Clear();
        CurrentChannels.Clear(); Servers.Clear();
        UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = "";
    }

    // NAVEGACAO
    [RelayCommand]
    public void OpenDmPanel()
    {
        IsInServer = false; _selectedServer = null; _activeChannel = null;
        IsChatOpen = _activeDmFriend != null; IsCreateServerOpen = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    public async Task SelectServer(ServerItem s)
    {
        if (s == null) return;
        _selectedServer = s; IsInServer = true; _activeDmFriend = null; IsCreateServerOpen = false;
        CurrentChannels.Clear();
        if (s.Channels != null) foreach (var c in s.Channels) CurrentChannels.Add(c);
        var first = CurrentChannels.FirstOrDefault(c => c.Type == ChannelType.Text);
        if (first != null) await SelectChannel(first); else IsChatOpen = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    // TABS
    [RelayCommand] public void ShowConversations() => DmTab = "conversations";
    [RelayCommand] public void ShowFriendRequests() => DmTab = "requests";

    // SERVIDOR
    [RelayCommand] public void ToggleCreateServer() { IsCreateServerOpen = !IsCreateServerOpen; NewServerName = ""; NewServerError = ""; }

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
                new ChannelItem { Id = 2, Name = "off-topic", Type = ChannelType.Text, Topic = "Assuntos livres"  },
            }
        };
        Servers.Add(server); NewServerName = ""; NewServerError = ""; IsCreateServerOpen = false;
        await SelectServer(server);
    }

    // CANAIS
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

    // AMIGOS
    [RelayCommand] public void ToggleAddFriend() { IsAddFriendOpen = !IsAddFriendOpen; AddFriendInput = ""; AddFriendError = ""; AddFriendSuccess = ""; }

    [RelayCommand]
    public async Task ConfirmAddFriend()
    {
        var name = AddFriendInput.Trim().ToLower();
        AddFriendError = ""; AddFriendSuccess = "";
        if (string.IsNullOrWhiteSpace(name)) { AddFriendError = "Digite um nome."; return; }
        if (name == CurrentUser.Username) { AddFriendError = "Voce nao pode se adicionar."; return; }
        if (Friends.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) { AddFriendError = "Ja esta na sua lista."; return; }

        if (_chatService.IsConnected)
            await _chatService.SendFriendRequestAsync(name);
        else
            AddFriendError = "Servidor offline.";
    }

    [RelayCommand]
    public async Task AcceptFriendRequest(FriendItem f)
    {
        if (f == null) return;
        PendingRequests.Remove(f);
        AddFriendToList(f.Name);
        await _chatService.AcceptFriendRequestAsync(f.Name);
    }

    [RelayCommand]
    public async Task DeclineFriendRequest(FriendItem f)
    {
        if (f == null) return;
        PendingRequests.Remove(f);
        await _chatService.DeclineFriendRequestAsync(f.Name);
    }

    [RelayCommand]
    public void RemoveFriend(FriendItem f)
    {
        if (f == null) return;
        if (_activeDmFriend == f) { _activeDmFriend = null; IsChatOpen = false; }
        Friends.Remove(f);
    }

    [RelayCommand]
    public async Task SelectFriend(FriendItem f)
    {
        if (f == null) return;
        _activeDmFriend = f; _activeChannel = null; IsInServer = false;
        CurrentChatName = f.Nickname.Length > 0 ? f.Nickname : f.Name;
        CurrentChatSubtitle = f.IsOnline ? "Online" : "Offline";
        IsChatOpen = true; ChatMessages.Clear(); IsAddFriendOpen = false;
        OnPropertyChanged(nameof(WindowTitle));
        var history = await _chatService.GetChatHistoryAsync(f.Name);
        Dispatcher.UIThread.Post(() => { foreach (var m in history) ChatMessages.Add(m); });
    }

    // MENSAGEM
    [RelayCommand]
    public async Task ProcessMessage()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        var msg = new MessageItem { Content = Input, Author = CurrentUser, Timestamp = DateTime.Now };
        Input = "";
        if (_activeChannel != null && _chatService.IsConnected && _selectedServer != null)
            await _chatService.SendMessageAsync(msg, _selectedServer.Name, _activeChannel.Name);
        else if (_activeDmFriend != null)
        {
            if (_chatService.IsConnected)
            {
                ChatMessages.Add(msg); // adiciona localmente
                await _chatService.SendPrivateMessageAsync(CurrentUser.Nickname, _activeDmFriend.Name, msg.Content);
            }
            else
                ChatMessages.Add(msg);
        }
        else
            _chatService.SimulateMessageReceived(msg);
    }

    // AUDIO
    [RelayCommand] public void ToggleMute()   { IsMuted = !IsMuted; SoundService.Play(IsMuted ? "pttoff" : "ptton"); }
    [RelayCommand] public void ToggleDeafen() { IsDeafened = !IsDeafened; IsMuted = IsDeafened; SoundService.Play(IsDeafened ? "deafen" : "undeafen"); }
    [RelayCommand] public void OpenSettings() { }

    // HELPERS
    private void AddFriendToList(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (Friends.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        Friends.Add(new FriendItem { Name = name, Nickname = name, Status = "Offline", Initials = name[0].ToString().ToUpper(), AvatarColor = AvatarColor(name) });
    }

    private void StartCloseTimer()
    {
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _timer.Tick += (_, _) => { _timer!.Stop(); AddFriendSuccess = ""; IsAddFriendOpen = false; };
        _timer.Start();
    }

    private static string AvatarColor(string name)
    {
        var c = new[] { "#5865F2","#57F287","#FEE75C","#EB459E","#ED4245","#00C9A7","#8B5CF6" };
        return c[Math.Abs(name.GetHashCode()) % c.Length];
    }
}
