using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace Void.ViewModels;

// Configurações da conta
public partial class MainViewModel
{
    [ObservableProperty] private bool _isSettingsOpen = false;
    [ObservableProperty] private string _settingsNickname = "";
    [ObservableProperty] private string _settingsAvatarColor = "#5865F2";
    [ObservableProperty] private string _settingsSaveStatus = "";
    private DispatcherTimer? _saveStatusTimer;

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
        if (string.IsNullOrWhiteSpace(nick)) { SettingsSaveStatus = "Nickname não pode ser vazio."; return; }
        if (nick.Length < 2) { SettingsSaveStatus = "Mínimo 2 caracteres."; return; }

        CurrentUser.Nickname = nick;
        CurrentUser.AvatarColor = SettingsAvatarColor;
        CurrentUser.Initials = nick.Length >= 2 ? nick[..2].ToUpper() : nick.ToUpper();
        OnPropertyChanged(nameof(CurrentUser));

        if (_chatService.IsConnected)
            await _chatService.UpdateProfileAsync(CurrentUser.Username, nick, SettingsAvatarColor);

        SettingsSaveStatus = "Salvo!";
        _saveStatusTimer?.Stop();
        _saveStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _saveStatusTimer.Tick += (_, _) => { _saveStatusTimer!.Stop(); SettingsSaveStatus = ""; IsSettingsOpen = false; };
        _saveStatusTimer.Start();
    }
}
