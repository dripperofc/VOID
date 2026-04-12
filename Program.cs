using Avalonia;
using System;
using System.IO;

namespace Void;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Captura exceções não tratadas em qualquer thread
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var msg = e.ExceptionObject?.ToString() ?? "Erro desconhecido";
            LogCrash("UnhandledException", msg);
        };

        // Captura exceções em Tasks async que não foram awaited
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("UnobservedTaskException", e.Exception?.ToString() ?? "?");
            e.SetObserved(); // evita fechar o app por causa disso
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogCrash("FatalException", ex.ToString());
            Console.WriteLine(ex.ToString());
            Console.WriteLine("\nPressione Enter para fechar...");
            Console.ReadLine();
        }
    }

    static void LogCrash(string tipo, string detalhe)
    {
        try
        {
            var logPath = "void_crash.log";
            var linha = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tipo}]\n{detalhe}\n{new string('-', 80)}\n";
            File.AppendAllText(logPath, linha);
            Console.WriteLine($"\n💥 CRASH [{tipo}]:\n{detalhe}");
        }
        catch { }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}