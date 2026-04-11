using Avalonia;
using Avalonia.Controls.ApplicationLifetimes; // <-- O "REMEDIO" PARA O SEU ERRO
using Avalonia.Markup.Xaml;
using Void.ViewModels;
using Void.Views;

namespace Void;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Criamos o ViewModel e passamos para a Janela Principal
            var vm = new MainViewModel();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}