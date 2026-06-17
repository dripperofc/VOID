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

public class ToastInfo
{
    public string Text { get; set; } = "";
    public string Type { get; set; } = "info";
    public string Color => Type switch { "err" => "#F04747", "ok" => "#43B581", _ => "#00C9A7" };
    public string BgColor => Type switch { "err" => "#F0474714", "ok" => "#43B58114", _ => "#00C9A714" };
}

public partial class MainViewModel : ObservableObject
{
    private readonly ChatService _chatService = new();
    private DispatcherTimer? _timer;

    // Toast system
    private int _toastId;
    public ObservableCollection<ToastInfo> Toasts { get; } = new();

    public void ShowToast(string text, string type = "info")
    {
        var id = ++_toastId;
        Toasts.Add(new ToastInfo { Text = text, Type = type });
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Toasts.RemoveAt(0);
        };
        timer.Start();
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
        var hash = 17;
        foreach (var ch in name) hash = hash * 31 + ch;
        return c[Math.Abs(hash) % c.Length];
    }

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

    // ── Navegação ─────────────────────────────────────────────────
    [ObservableProperty] private bool _isInServer = false;
    [ObservableProperty] private bool _isChatOpen = false;
    [ObservableProperty] private bool _isAddFriendOpen = false;
    [ObservableProperty] private bool _isCreateServerOpen = false;

    // Aba DMs
    [ObservableProperty] private string _dmTab = "conversations";
    public bool IsConversationsTab  => DmTab == "conversations";
    public bool IsFriendRequestsTab => DmTab == "requests";
    public int  PendingCount        => PendingRequests.Count;
    public bool HasPending          => PendingRequests.Count > 0;
    partial void OnDmTabChanged(string value) { OnPropertyChanged(nameof(IsConversationsTab)); OnPropertyChanged(nameof(IsFriendRequestsTab)); }

    // Chat ativo
    [ObservableProperty] private string _currentChatName = "";
    [ObservableProperty] private string _currentChatSubtitle = "";
    private FriendItem? _activeDmFriend;
    private ChannelItem? _activeChannel;
    private ServerItem? _selectedServer;
    private string? _lastActiveFriend;

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
            if (msg.Author?.Nickname == CurrentUser.Nickname || msg.Author?.Username == CurrentUser.Username) return;
            var fromName = msg.Author?.Username ?? msg.Author?.Nickname ?? "";
            var isActive = _activeDmFriend != null && _activeDmFriend.Name.Equals(fromName, StringComparison.OrdinalIgnoreCase);
            if (isActive)
            {
                ChatMessages.Add(msg);
            }
            else
            {
                var friend = Friends.FirstOrDefault(f => f.Name.Equals(fromName, StringComparison.OrdinalIgnoreCase));
                if (friend != null) friend.UnreadCount++;
            }
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

        _chatService.BroadcastReceived += msg => Dispatcher.UIThread.Post(() =>
            ShowToast($"📢 {msg}", "ok"));

        _chatService.Kicked += () => Dispatcher.UIThread.Post(async () =>
        {
            ShowToast("Desconectado pelo admin.", "err");
            await Task.Delay(2000);
            await Logout();
        });

        _chatService.Reconnecting += () => Dispatcher.UIThread.Post(() =>
            ShowToast("Reconectando...", "info"));

        _chatService.Reconnected += () => Dispatcher.UIThread.Post(() =>
            ShowToast("Reconectado.", "ok"));
    }
}
