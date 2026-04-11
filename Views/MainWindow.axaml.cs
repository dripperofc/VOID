using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml; // <--- Novo using necessário
using Avalonia.Threading;
using System.Collections.Specialized;
using Void.ViewModels;

namespace Void.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Se o InitializeComponent() sumiu, a gente chama o Loader na mão:
        AvaloniaXamlLoader.Load(this); 
        
        // Lógica do Scroll Automático
        this.DataContextChanged += (s, e) => {
            if (this.DataContext is MainViewModel vm)
            {
                vm.ChatMessages.CollectionChanged += (sender, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        Dispatcher.UIThread.Post(() => {
                            var scroll = this.FindControl<ScrollViewer>("ChatScroll");
                            scroll?.ScrollToEnd();
                        }, DispatcherPriority.Background);
                    }
                };
            }
        };
    }

    // Se o compilador reclamar que InitializeComponent não existe, 
    // a gente cria um método vazio só para calar a boca dele
    private void InitializeComponent() { } 
}