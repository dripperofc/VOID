using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Void.Models;

namespace Void.ViewModels;

// Servidores, canais e convites
public partial class MainViewModel
{
    [ObservableProperty] private string _newServerName = "";
    [ObservableProperty] private string _newServerError = "";
    [ObservableProperty] private string _inviteCode = "";
    [ObservableProperty] private string _inviteError = "";
    [ObservableProperty] private string _inviteSuccess = "";

    public ObservableCollection<ServerItem>  Servers         { get; } = new();
    public ObservableCollection<ChannelItem> CurrentChannels { get; } = new();

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

    [RelayCommand]
    public void ToggleCreateServer() { IsCreateServerOpen = !IsCreateServerOpen; NewServerName = ""; NewServerError = ""; }

    [RelayCommand]
    public async Task ConfirmCreateServer()
    {
        var name = NewServerName.Trim();
        if (string.IsNullOrWhiteSpace(name)) { NewServerError = "Digite um nome."; return; }
        if (name.Length < 2) { NewServerError = "Nome muito curto."; return; }

        if (_chatService.IsConnected)
        {
            var server = await _chatService.CreateServerAsync(name);
            if (server != null)
            {
                Servers.Add(server);
                NewServerName = ""; NewServerError = ""; IsCreateServerOpen = false;
                await SelectServer(server);
            }
            else NewServerError = "Erro ao criar servidor.";
        }
        else
        {
            var server = new ServerItem
            {
                Id = Servers.Count + 1, ServerId = name, Name = name, OwnerId = CurrentUser.Id,
                Channels = new() { new ChannelItem { Name = "geral", Type = ChannelType.Text } }
            };
            Servers.Add(server); NewServerName = ""; NewServerError = ""; IsCreateServerOpen = false;
            await SelectServer(server);
        }
    }

    [RelayCommand]
    public async Task JoinByCode()
    {
        var code = InviteCode.Trim().ToUpper();
        InviteError = ""; InviteSuccess = "";
        if (string.IsNullOrWhiteSpace(code)) { InviteError = "Digite um codigo."; return; }
        if (!_chatService.IsConnected) { InviteError = "Servidor offline."; return; }

        var ok = await _chatService.JoinServerAsync(code);
        if (ok)
        {
            InviteSuccess = $"Entrou no servidor!";
            InviteCode = "";
            await LoadServers();
        }
        else InviteError = "Codigo invalido.";
    }

    [RelayCommand]
    public async Task SelectChannel(ChannelItem c)
    {
        if (c == null) return;
        _activeChannel = c; _activeDmFriend = null;
        CurrentChatName = c.Name; CurrentChatSubtitle = c.Topic ?? "Canal de texto";
        IsChatOpen = true; ChatMessages.Clear();
        OnPropertyChanged(nameof(IsDmChatActive));
        if (_chatService.IsConnected && _selectedServer != null)
        {
            await _chatService.JoinChannelAsync(_selectedServer.ServerId, c.Name);
            var history = await _chatService.GetChannelMessagesAsync(_selectedServer.ServerId, c.Name);
            foreach (var msg in history)
                ChatMessages.Add(msg);
        }
    }
}
