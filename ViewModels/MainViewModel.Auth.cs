using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models;

namespace Void.ViewModels;

// Autenticação: login, registro, sessão
public partial class MainViewModel
{
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isRegisterView = false;
    [ObservableProperty] private bool _isLoading = false;

    [ObservableProperty] private string _usernameInput = "";
    [ObservableProperty] private string _nicknameInput = "";
    [ObservableProperty] private string _passwordInput = "";
    [ObservableProperty] private string _loginError = "";

    public UserProfile CurrentUser { get; private set; } = new();

    [RelayCommand]
    public void ToggleView() { IsRegisterView = !IsRegisterView; UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = ""; }

    [RelayCommand]
    public async Task ConfirmLogin()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) { LoginError = "Preencha usuario e senha."; return; }
        IsLoading = true; LoginError = "";
        var result = await _chatService.AuthenticateAsync(UsernameInput.Trim().ToLower(), PasswordInput, false);
        IsLoading = false;
        if (result == "ok" || (!string.IsNullOrEmpty(result) && !result.StartsWith("error") && !result.Contains("invalid") && !result.Contains("exists") && result.Length > 20))
        {
            var token = result == "ok" ? null : result;
            await EnterApp(UsernameInput.Trim().ToLower(), token);
        }
        else if (result == "invalid_credentials") LoginError = "Usuario ou senha incorretos.";
        else LoginError = "Servidor offline ou erro desconhecido.";
    }

    [RelayCommand]
    public async Task ConfirmRegister()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput)) { LoginError = "Preencha usuario e senha."; return; }
        if (PasswordInput.Length < 4) { LoginError = "Senha: minimo 4 caracteres."; return; }
        IsLoading = true; LoginError = "";
        var nickname = NicknameInput.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(nickname)) nickname = UsernameInput.Trim().ToLower();
        var result = await _chatService.AuthenticateAsync(UsernameInput.Trim().ToLower(), PasswordInput, true, nickname);
        IsLoading = false;
        if (result == "ok" || (!string.IsNullOrEmpty(result) && !result.StartsWith("error") && !result.Contains("invalid") && !result.Contains("exists") && result.Length > 20))
        {
            var token = result == "ok" ? null : result;
            await EnterApp(UsernameInput.Trim().ToLower(), token);
        }
        else if (result == "user_exists") LoginError = "Usuario ja existe.";
        else if (result == "invalid_username_length") LoginError = "Usuario: 3-32 caracteres.";
        else if (result == "invalid_password_length") LoginError = "Senha: 4-128 caracteres.";
        else LoginError = "Servidor offline. Nao e possivel criar conta.";
    }

    private async Task EnterApp(string username, string? token)
    {
        var profile = await _chatService.GetUserProfileAsync(username);
        CurrentUser = new UserProfile
        {
            Username  = username,
            Nickname  = profile?.Nickname ?? username,
            AvatarColor = profile?.AvatarColor ?? "#5865F2",
            Initials  = profile?.Initials ?? username[0].ToString().ToUpper()
        };
        OnPropertyChanged(nameof(CurrentUser));

        if (profile?.Friends != null)
            foreach (var f in profile.Friends)
            {
                var friendProfile = await _chatService.GetUserProfileAsync(f);
                AddFriendToList(f, friendProfile?.IsOnline ?? false);
            }

        var pending = await _chatService.GetPendingRequestsAsync(username);
        foreach (var p in pending)
            if (!PendingRequests.Any(x => x.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
                PendingRequests.Add(new FriendItem { Name = p, Nickname = p, Status = "Online", Initials = p[0].ToString().ToUpper(), AvatarColor = AvatarColor(p) });

        IsLoggedIn = true;
        SetupVoiceEvents();

        _chatService.FriendsPresenceReceived += onlineList => Dispatcher.UIThread.Post(() =>
        {
            foreach (var name in onlineList)
            {
                var f = Friends.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (f != null) f.Status = "Online";
            }
        });

        await _chatService.NotifyOnlineAsync(username);

        if (_chatService.IsConnected)
        {
            await _chatService.JoinOfficialServerAsync();
            await LoadServers();
        }

        OpenDmPanel();
    }

    [RelayCommand]
    public async Task Logout()
    {
        _timer?.Stop();
        StopCallTimer();
        Toasts.Clear();
        _activeDmFriend = null;
        _lastActiveFriend = null;
        await _chatService.NotifyOfflineAsync(CurrentUser.Username);
        await _chatService.DisconnectAsync();
        IsLoggedIn = false; IsChatOpen = false; DmTab = "conversations";
        ChatMessages.Clear(); Friends.Clear(); PendingRequests.Clear();
        CurrentChannels.Clear(); Servers.Clear();
        InviteCode = ""; InviteError = ""; InviteSuccess = "";
        UsernameInput = ""; PasswordInput = ""; NicknameInput = ""; LoginError = "";
    }

    private async Task LoadServers()
    {
        if (!_chatService.IsConnected) return;
        var servers = await _chatService.GetServersAsync();
        Dispatcher.UIThread.Post(() =>
        {
            Servers.Clear();
            foreach (var s in servers)
            {
                if (!Servers.Any(x => x.ServerId == s.ServerId))
                    Servers.Add(s);
            }
        });
    }
}
