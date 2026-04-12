using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Void.Services.Interfaces;

namespace Void.Services;

public class AudioService : IAudioService, IDisposable
{
    private readonly ILoggingService _logger;
    private readonly SemaphoreSlim _audioLock = new(1, 1);
    private readonly ConcurrentQueue<byte[]> _audioQueue = new();
    
    private WaveInEvent? _waveIn;
    private WasapiOut? _waveOut;
    private BufferedWaveProvider? _waveProvider;
    private CancellationTokenSource? _cts;
    
    public bool IsCapturing { get; private set; }
    public bool IsPlaying { get; private set; }
    
    public event EventHandler<byte[]>? AudioDataReceived;
    
    public AudioService(ILoggingService logger)
    {
        _logger = logger;
        _logger.Info("🎤 AudioService inicializado");
    }
    
    public async Task StartVoiceCaptureAsync()
    {
        await _audioLock.WaitAsync();
        try
        {
            if (IsCapturing)
                return;
                
            _cts = new CancellationTokenSource();
            
            await Task.Run(() =>
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(48000, 16, 1),
                    BufferMilliseconds = 20
                };
                
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.StartRecording();
                IsCapturing = true;
                
                _logger.Info("🎙️ Captura de voz iniciada");
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Erro ao iniciar captura de voz");
        }
        finally
        {
            _audioLock.Release();
        }
    }
    
    public async Task StopVoiceCaptureAsync()
    {
        await _audioLock.WaitAsync();
        try
        {
            if (!IsCapturing || _waveIn == null)
                return;
                
            _cts?.Cancel();
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.Dispose();
            _waveIn = null;
            IsCapturing = false;
            
            _logger.Info("🛑 Captura de voz finalizada");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Erro ao parar captura de voz");
        }
        finally
        {
            _audioLock.Release();
        }
    }
    
    public async Task StartAudioPlaybackAsync(byte[] audioData)
    {
        await _audioLock.WaitAsync();
        try
        {
            if (_waveOut == null)
            {
                _waveOut = new WasapiOut();
                _waveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));
                _waveOut.Init(_waveProvider);
                _waveOut.Play();
                IsPlaying = true;
            }
            
            _waveProvider?.AddSamples(audioData, 0, audioData.Length);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Erro ao reproduzir áudio");
        }
        finally
        {
            _audioLock.Release();
        }
    }
    
    public void SetVolume(float volume)
    {
        if (_waveOut != null)
        {
            _waveOut.Volume = Math.Clamp(volume, 0f, 1f);
        }
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            var audioData = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, audioData, e.BytesRecorded);
            
            AudioDataReceived?.Invoke(this, audioData);
        }
    }
    
    public void Dispose()
    {
        _waveIn?.Dispose();
        _waveOut?.Dispose();
        _cts?.Dispose();
        _audioLock.Dispose();
        
        _logger.Info("♻️ AudioService liberado");
    }
}