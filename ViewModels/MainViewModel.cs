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

        // FIX: busca presença real do amigo recém-aceito em vez de entrar sempre como Offline
        _chatService.FriendAccepted += async friend =>
        {
            var profile = await _chatService.GetUserProfileAsync(friend);
            Dispatcher.UIThread.Post(() =>
            {
                AddFriendToList(friend, profile?.IsOnline ?? false);
                SoundService.Play("join");
            });
        };

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

        // Carrega lista de amigos do perfil com status de presença real
        if (profile?.Friends != null)
            foreach (var f in profile.Friends)
            {
                var friendProfile = await _chatService.GetUserProfileAsync(f);
                AddFriendToList(f, friendProfile?.IsOnline ?? false);
            }

        // Carrega pedidos pendentes
        var pending = await _chatService.GetPendingRequestsAsync(username);
        foreach (var p in pending)
            if (!PendingRequests.Any(x => x.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
                PendingRequests.Add(new FriendItem { Name = p, Nickname = p, Status = "Online", Initials = p[0].ToString().ToUpper(), AvatarColor = AvatarColor(p) });

        IsLoggedIn = true;
        SetupVoiceEvents(); // inicializa eventos de chamada de voz

        // Escuta o snapshot de presença que o servidor envia após NotifyOnline.
        // Atualiza de uma vez todos os amigos que já estão online,
        // sem precisar de N chamadas GetUserProfile.
        _chatService.FriendsPresenceReceived += onlineList => Dispatcher.UIThread.Post(() =>
        {
            foreach (var name in onlineList)
            {
                var f = Friends.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (f != null) f.Status = "Online";
            }
        });

        // Avisa o servidor que este usuário está online agora.
        // O servidor propaga UserStatusChanged para os amigos dele que já estão logados,
        // e envia FriendsPresenceSnapshot de volta com quem já está online.
        await _chatService.NotifyOnlineAsync(username);

        OpenDmPanel();
    }

    [RelayCommand]
    public async Task Logout()
    {
        _timer?.Stop();
        // Avisa os amigos que saiu antes de desconectar
        await _chatService.NotifyOfflineAsync(CurrentUser.Username);
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
        OnPropertyChanged(nameof(IsDmChatActive)); // FIX: esconde botão de chamada em canais
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
        // FIX: busca presença real — quem mandou o pedido provavelmente está online
        var profile = await _chatService.GetUserProfileAsync(f.Name);
        AddFriendToList(f.Name, profile?.IsOnline ?? false);
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
        OnPropertyChanged(nameof(IsDmChatActive)); // FIX: atualiza visibilidade do botão de chamada
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


    // CHAMADAS DE VOZ
    [RelayCommand]
    public async Task ToggleVoiceCall()
    {
        var voice = _chatService.Voice;
        if (voice == null) { CallStatus = "Conecte-se primeiro"; return; }
        if (_activeDmFriend == null) { CallStatus = "Selecione um amigo para ligar"; return; }

        if (IsInCall)
        {
            await voice.HangUpAsync();
            IsInCall = false;
            CallStatus = "";
        }
        else
        {
            CallStatus = $"Chamando {_activeDmFriend.Nickname}...";
            await voice.CallAsync(_activeDmFriend.Name);
        }
    }

    [RelayCommand]
    public async Task AcceptIncomingCall()
    {
        var voice = _chatService.Voice;
        if (voice == null) return;
        await voice.AcceptCallAsync();
        IsInCall = true;
        IncomingCallVisible = false;
        CallStatus = $"Em chamada com {voice.CurrentPeer}";
    }

    [RelayCommand]
    public async Task DeclineIncomingCall()
    {
        var voice = _chatService.Voice;
        if (voice == null) return;
        await voice.DeclineCallAsync();
        IncomingCallVisible = false;
        CallStatus = "";
    }

    private void SetupVoiceEvents()
    {
        var voice = _chatService.Voice;
        if (voice == null) return;

        voice.IncomingCall += caller => Dispatcher.UIThread.Post(() =>
        {
            IncomingCallerName = caller;
            IncomingCallVisible = true;
            SoundService.Play("ptton");
        });

        voice.CallAccepted += peer => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = true;
            CallStatus = $"Em chamada com {peer}";
        });

        voice.CallDeclined += peer => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = false;
            CallStatus = $"{peer} recusou a chamada";
        });

        voice.CallEnded += peer => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = false;
            CallStatus = "";
        });

        // FIX: propaga erros de chamada para a UI em vez de sumir no vazio
        voice.CallError += error => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = false;
            CallStatus = $"Erro na chamada: {error}";
        });
    }

    // AUDIO
    [RelayCommand] public void ToggleMute()   { IsMuted = !IsMuted; _chatService.Voice?.SetMuted(IsMuted); SoundService.Play(IsMuted ? "pttoff" : "ptton"); }
    [RelayCommand] public void ToggleDeafen() { IsDeafened = !IsDeafened; IsMuted = IsDeafened; SoundService.Play(IsDeafened ? "deafen" : "undeafen"); }
    // ── CONFIGURAÇÕES DE CONTA ────────────────────────────────────────────
    [ObservableProperty] private bool _isSettingsOpen = false;
    [ObservableProperty] private string _settingsNickname = "";
    [ObservableProperty] private string _settingsAvatarColor = "#5865F2";
    [ObservableProperty] private string _settingsSaveStatus = "";
    private DispatcherTimer? _saveStatusTimer;

    // Paleta de cores do avatar para o usuário escolher
    public string[] AvatarColorOptions { get; } = new[]
    {
        "#5865F2", "#57F287", "#FEE75C", "#EB459E",
        "#ED4245", "#00C9A7", "#8B5CF6", "#FF7043"
    };

    [RelayCommand]
    public void OpenSettings()
    {
        SettingsNickname = CurrentUser.Nickname;
        SettingsAvatarColor = CurrentUser.AvatarColor;
        SettingsSaveStatus = "";
        IsSettingsOpen = true;
    }

    [RelayCommand]
    public void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    public void SelectAvatarColor(string color) => SettingsAvatarColor = color;

    [RelayCommand]
    public async Task SaveSettings()
    {
        var nick = SettingsNickname.Trim();
        if (string.IsNullOrWhiteSpace(nick)) { SettingsSaveStatus = "❌ Nickname não pode ser vazio."; return; }
        if (nick.Length < 2) { SettingsSaveStatus = "❌ Mínimo 2 caracteres."; return; }

        CurrentUser.Nickname = nick;
        CurrentUser.AvatarColor = SettingsAvatarColor;
        CurrentUser.Initials = nick.Length >= 2 ? nick[..2].ToUpper() : nick.ToUpper();
        OnPropertyChanged(nameof(CurrentUser));

        // Salva no servidor via SignalR se conectado
        if (_chatService.IsConnected)
            await _chatService.UpdateProfileAsync(CurrentUser.Username, nick, SettingsAvatarColor);

        SettingsSaveStatus = "✅ Salvo!";
        _saveStatusTimer?.Stop();
        _saveStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _saveStatusTimer.Tick += (_, _) => { _saveStatusTimer!.Stop(); SettingsSaveStatus = ""; IsSettingsOpen = false; };
        _saveStatusTimer.Start();
    }


    // ── CHAMADAS DE VOZ ───────────────────────────────────────────────────
    private bool _isInCall = false;
    public bool IsInCall { get => _isInCall; set { SetProperty(ref _isInCall, value); OnPropertyChanged(nameof(CallButtonText)); } }

    private string _callStatus = "";
    public string CallStatus
    {
        get => _callStatus;
        set
        {
            if (SetProperty(ref _callStatus, value))
                OnPropertyChanged(nameof(HasCallStatus));
        }
    }

    // FIX: mostra o status "Chamando..." e erros ANTES da chamada conectar
    public bool HasCallStatus => !string.IsNullOrWhiteSpace(CallStatus);

    // FIX: botão de chamada só aparece em DMs, não em canais de servidor
    public bool IsDmChatActive => _activeDmFriend != null;

    private bool _incomingCallVisible = false;
    public bool IncomingCallVisible { get => _incomingCallVisible; set => SetProperty(ref _incomingCallVisible, value); }

    private string _incomingCallerName = "";
    public string IncomingCallerName { get => _incomingCallerName; set => SetProperty(ref _incomingCallerName, value); }

    public string CallButtonText => IsInCall ? "Desligar" : "Ligar";

    // HELPERS
    // FIX: aceita o estado real de presença em vez de sempre criar como Offline
    private void AddFriendToList(string name, bool isOnline = false)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (Friends.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        Friends.Add(new FriendItem
        {
            Name = name,
            Nickname = name,
            Status = isOnline ? "Online" : "Offline",
            Initials = name[0].ToString().ToUpper(),
            AvatarColor = AvatarColor(name)
        });
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
