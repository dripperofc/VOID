using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Void.Services;

/// <summary>
/// Gerencia chamadas de voz P2P via SignalR como sinalização + NAudio para captura/reprodução.
/// Fluxo: Caller inicia → Servidor avisa o alvo → Alvo aceita/recusa → Ambos trocam áudio.
/// </summary>
public class VoiceCallService : IDisposable
{
    // ── Estado ────────────────────────────────────────────────────────────
    public bool IsInCall        { get; private set; }
    public bool IsMuted         { get; set; }
    public string? CurrentPeer  { get; private set; }
    public CallState State      { get; private set; } = CallState.Idle;

    // ── Eventos para o ViewModel ──────────────────────────────────────────
    public event Action<string>? IncomingCall;      // username do chamador
    public event Action<string>? CallAccepted;      // peer aceitou
    public event Action<string>? CallDeclined;      // peer recusou
    public event Action<string>? CallEnded;         // chamada encerrada
    public event Action<string>? CallError;         // erro

    // ── Dependências ──────────────────────────────────────────────────────
    private readonly LoggingService        _log;
    private readonly HubConnection          _hub;
    private readonly SemaphoreSlim          _lock = new(1, 1);

    // ── NAudio ────────────────────────────────────────────────────────────
    private WaveInEvent?                    _waveIn;
    private WasapiOut?                      _waveOut;
    private BufferedWaveProvider?           _waveProvider;
    private static readonly WaveFormat     AudioFmt = new(48000, 16, 1);

    // Buffer de áudio recebido de peers (username → amostras)
    private readonly ConcurrentQueue<byte[]> _playbackQueue = new();

    // ── Construtor ────────────────────────────────────────────────────────
    public VoiceCallService(LoggingService log, HubConnection hub)
    {
        _log = log;
        _hub = hub;
        RegisterHandlers();
    }

    // ── Registro de eventos SignalR ───────────────────────────────────────
    private void RegisterHandlers()
    {
        _hub.On<string>("VoiceCallIncoming", caller =>
        {
            _log.Info($"📞 Chamada recebida de {caller}");
            State = CallState.Ringing;
            CurrentPeer = caller;
            IncomingCall?.Invoke(caller);
        });

        _hub.On<string>("VoiceCallAccepted", peer =>
        {
            _log.Info($"✅ {peer} aceitou a chamada");
            State = CallState.InCall;
            IsInCall = true;
            _ = StartAudioStreaming();
            CallAccepted?.Invoke(peer);
        });

        _hub.On<string>("VoiceCallDeclined", peer =>
        {
            _log.Info($"❌ {peer} recusou a chamada");
            Reset();
            CallDeclined?.Invoke(peer);
        });

        _hub.On<string>("VoiceCallEnded", peer =>
        {
            _log.Info($"🔴 Chamada encerrada por {peer}");
            _ = StopAudioStreaming();
            Reset();
            CallEnded?.Invoke(peer);
        });

        // Recebe chunk de áudio do peer
        // FIX: IsMuted silencia o SEU microfone, não o áudio que você ouve
        _hub.On<string, byte[]>("VoiceAudioChunk", (from, data) =>
        {
            if (IsInCall)
                _playbackQueue.Enqueue(data);
        });
    }

    // ── API Pública ───────────────────────────────────────────────────────

    public async Task CallAsync(string targetUsername)
    {
        if (IsInCall || State != CallState.Idle) return;
        await _lock.WaitAsync();
        try
        {
            CurrentPeer = targetUsername;
            State = CallState.Calling;
            await _hub.InvokeAsync("StartVoiceCall", targetUsername);
            _log.Info($"📞 Chamando {targetUsername}...");
        }
        catch (Exception ex)
        {
            Reset();
            CallError?.Invoke(ex.Message);
        }
        finally { _lock.Release(); }
    }

    public async Task AcceptCallAsync()
    {
        if (State != CallState.Ringing || CurrentPeer == null) return;
        await _hub.InvokeAsync("AcceptVoiceCall", CurrentPeer);
        State = CallState.InCall;
        IsInCall = true;
        _ = StartAudioStreaming();
        _log.Info($"✅ Chamada com {CurrentPeer} aceita");
    }

    public async Task DeclineCallAsync()
    {
        if (State != CallState.Ringing || CurrentPeer == null) return;
        await _hub.InvokeAsync("DeclineVoiceCall", CurrentPeer);
        Reset();
    }

    public async Task HangUpAsync()
    {
        if (!IsInCall && State == CallState.Idle) return;
        var peer = CurrentPeer;
        await StopAudioStreaming();
        if (peer != null)
            await _hub.InvokeAsync("EndVoiceCall", peer);
        Reset();
        if (peer != null) CallEnded?.Invoke(peer);
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        if (_waveIn != null)
        {
            if (muted) _waveIn.StopRecording();
            else       _waveIn.StartRecording();
        }
    }

    // ── Streaming de Áudio ────────────────────────────────────────────────

    private async Task StartAudioStreaming()
    {
        await _lock.WaitAsync();
        try
        {
            // Captura (microfone → SignalR)
            _waveIn = new WaveInEvent { WaveFormat = AudioFmt, BufferMilliseconds = 40 };
            _waveIn.DataAvailable += OnMicData;
            if (!IsMuted) _waveIn.StartRecording();

            // Reprodução (buffer → alto-falante)
            _waveOut      = new WasapiOut();
            _waveProvider = new BufferedWaveProvider(AudioFmt) { DiscardOnBufferOverflow = true };
            _waveOut.Init(_waveProvider);
            _waveOut.Play();

            // Loop de playback
            _ = Task.Run(PlaybackLoop);

            _log.Info("🎙️ Streaming de áudio iniciado");
        }
        catch (Exception ex) { _log.Error(ex, "Erro ao iniciar streaming de áudio"); }
        finally { _lock.Release(); }
    }

    private async Task StopAudioStreaming()
    {
        await _lock.WaitAsync();
        try
        {
            _waveIn?.StopRecording();
            _waveIn?.Dispose(); _waveIn = null;
            _waveOut?.Stop();
            _waveOut?.Dispose(); _waveOut = null;
            _waveProvider = null;
            while (_playbackQueue.TryDequeue(out _)) { }
            _log.Info("🛑 Streaming de áudio encerrado");
        }
        finally { _lock.Release(); }
    }

    private async void OnMicData(object? sender, WaveInEventArgs e)
    {
        if (IsMuted || !IsInCall || CurrentPeer == null || e.BytesRecorded == 0) return;
        var chunk = new byte[e.BytesRecorded];
        Buffer.BlockCopy(e.Buffer, 0, chunk, 0, e.BytesRecorded);
        try { await _hub.InvokeAsync("SendVoiceAudio", CurrentPeer, chunk); }
        catch { /* ignora falhas de rede pontuais */ }
    }

    private async Task PlaybackLoop()
    {
        while (IsInCall)
        {
            while (_playbackQueue.TryDequeue(out var chunk))
                _waveProvider?.AddSamples(chunk, 0, chunk.Length);
            await Task.Delay(20);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void Reset()
    {
        IsInCall    = false;
        CurrentPeer = null;
        State       = CallState.Idle;
    }

    public void Dispose()
    {
        _waveIn?.Dispose();
        _waveOut?.Dispose();
        _lock.Dispose();
    }
}

public enum CallState { Idle, Calling, Ringing, InCall }
