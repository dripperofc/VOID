using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Models;

namespace Void.ViewModels;

// Gerenciamento de amigos e DMs
public partial class MainViewModel
{
    [ObservableProperty] private string _addFriendInput = "";
    [ObservableProperty] private string _addFriendError = "";
    [ObservableProperty] private string _addFriendSuccess = "";

    public ObservableCollection<FriendItem> Friends         { get; } = new();
    public ObservableCollection<FriendItem> PendingRequests { get; } = new();

    [RelayCommand]
    public void ToggleAddFriend() { IsAddFriendOpen = !IsAddFriendOpen; AddFriendInput = ""; AddFriendError = ""; AddFriendSuccess = ""; }

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
        f.UnreadCount = 0;
        _lastActiveFriend = f.Name;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsDmChatActive));
        var history = await _chatService.GetChatHistoryAsync(f.Name);
        Dispatcher.UIThread.Post(() => { foreach (var m in history) ChatMessages.Add(m); });
    }

    [RelayCommand]
    public void ShowConversations() => DmTab = "conversations";

    [RelayCommand]
    public void ShowFriendRequests() => DmTab = "requests";
}
