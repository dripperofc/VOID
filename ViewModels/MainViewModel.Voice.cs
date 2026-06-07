using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Void.Services;

namespace Void.ViewModels;

// Chamadas de voz e controle de áudio
public partial class MainViewModel
{
    // ── Estado de áudio ────────────────────────────────────────────
    private bool _isMuted = false;
    public bool IsMuted { get => _isMuted; set { SetProperty(ref _isMuted, value); OnPropertyChanged(nameof(MuteIcon)); OnPropertyChanged(nameof(MuteColor)); SoundService.Muted = value; } }
    private bool _isDeafened = false;
    public bool IsDeafened { get => _isDeafened; set { SetProperty(ref _isDeafened, value); OnPropertyChanged(nameof(DeafenIcon)); OnPropertyChanged(nameof(DeafenColor)); } }
    public string MuteIcon    => IsMuted    ? "X" : "M";
    public string DeafenIcon  => IsDeafened ? "X" : "H";
    public string MuteColor   => IsMuted    ? "#F04747" : "#6B7280";
    public string DeafenColor => IsDeafened ? "#F04747" : "#6B7280";

    // ── Estado da chamada ───────────────────────────────────────────
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

    public bool HasCallStatus => !string.IsNullOrWhiteSpace(CallStatus);
    public bool IsDmChatActive => _activeDmFriend != null;

    private bool _incomingCallVisible = false;
    public bool IncomingCallVisible { get => _incomingCallVisible; set => SetProperty(ref _incomingCallVisible, value); }

    private string _incomingCallerName = "";
    public string IncomingCallerName { get => _incomingCallerName; set => SetProperty(ref _incomingCallerName, value); }

    public string CallButtonText => IsInCall ? "Desligar" : "Ligar";

    // ── Call timer ──────────────────────────────────────────────────
    [ObservableProperty] private string _callDuration = "00:00";
    private DispatcherTimer? _callTimer;
    private DateTime _callStartTime;

    private void StartCallTimer()
    {
        _callStartTime = DateTime.Now;
        _callTimer?.Stop();
        _callTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _callTimer.Tick += (_, _) =>
        {
            var elapsed = DateTime.Now - _callStartTime;
            CallDuration = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        };
        _callTimer.Start();
    }

    private void StopCallTimer()
    {
        _callTimer?.Stop();
        _callTimer = null;
        CallDuration = "00:00";
    }

    // ── Comandos de áudio ──────────────────────────────────────────
    [RelayCommand]
    public void ToggleMute()   { IsMuted = !IsMuted; _chatService.Voice?.SetMuted(IsMuted); SoundService.Play(IsMuted ? "pttoff" : "ptton"); }

    [RelayCommand]
    public void ToggleDeafen() { IsDeafened = !IsDeafened; IsMuted = IsDeafened; SoundService.Play(IsDeafened ? "deafen" : "undeafen"); }

    // ── Comandos de chamada ────────────────────────────────────────
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
            StartCallTimer();
        });

        voice.CallDeclined += peer => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = false;
            CallStatus = $"{peer} recusou a chamada";
            StopCallTimer();
        });

        voice.CallEnded += peer => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = false;
            CallStatus = "";
            StopCallTimer();
        });

        voice.CallError += error => Dispatcher.UIThread.Post(() =>
        {
            IsInCall = false;
            CallStatus = $"Erro na chamada: {error}";
            StopCallTimer();
        });
    }
}
