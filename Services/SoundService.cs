using System;
using System.IO;
using NAudio.Wave;

namespace Void.Services;

/// <summary>
/// Toca sons de notificação da pasta sounds/.
/// Uso: SoundService.Play("message") — sem extensão, sem caminho.
/// </summary>
public static class SoundService
{
    private static float _volume = 1.0f;
    private static bool _muted = false;

    public static float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public static bool Muted
    {
        get => _muted;
        set => _muted = value;
    }

    public static void Play(string soundName)
    {
        if (_muted) return;

        try
        {
            var path = Path.Combine("sounds", $"{soundName}.mp3");
            if (!File.Exists(path)) return;

            // Roda em thread separada para não bloquear a UI
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    using var reader = new Mp3FileReader(path);
                    using var output = new WaveOutEvent();
                    output.Volume = _volume;
                    output.Init(reader);
                    output.Play();
                    // Aguarda terminar (max 5s para não travar)
                    var timeout = DateTime.Now.AddSeconds(5);
                    while (output.PlaybackState == PlaybackState.Playing && DateTime.Now < timeout)
                        System.Threading.Thread.Sleep(50);
                }
                catch { /* Som falhou silenciosamente */ }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        catch { /* Ignora erros de som */ }
    }
}