using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Void.Models;

namespace Void.ViewModels;

// Envio e recebimento de mensagens
public partial class MainViewModel
{
    [ObservableProperty] private string _input = "";

    public ObservableCollection<MessageItem> ChatMessages { get; } = new();

    [RelayCommand]
    public async Task ProcessMessage()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        var msg = new MessageItem { Content = Input, Author = CurrentUser, Timestamp = System.DateTime.Now };
        Input = "";
        if (_activeChannel != null && _chatService.IsConnected && _selectedServer != null)
            await _chatService.SendMessageAsync(msg, _selectedServer.ServerId, _activeChannel.Name);
        else if (_activeDmFriend != null)
        {
            if (_chatService.IsConnected)
            {
                ChatMessages.Add(msg);
                await _chatService.SendPrivateMessageAsync(CurrentUser.Nickname, _activeDmFriend.Name, msg.Content);
            }
            else
                ChatMessages.Add(msg);
        }
        else
            _chatService.SimulateMessageReceived(msg);
    }
}
