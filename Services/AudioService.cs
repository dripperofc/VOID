using System;
using System.IO;
using NetCoreAudio;

namespace Void.Services;

public static class AudioService
{
    private static readonly Player _player = new Player();

    public static void Play(string fileName)
    {
        try
        {
            // Pega o caminho de onde o app está rodando
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            // Procura na sua pasta "sounds" (exatamente como você escreveu)
            string filePath = Path.Combine(basePath, "sounds", fileName);

            if (File.Exists(filePath))
            {
                // Toca o som sem travar a tela (Fire and forget)
                _ = _player.Play(filePath);
            }
            else
            {
                Console.WriteLine($"[VOID AUDIO] Erro: O arquivo {fileName} não foi encontrado em {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VOID AUDIO] Falha ao reproduzir: {ex.Message}");
        }
    }
}