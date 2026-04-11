using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Void.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Esta linha faz exatamente o que o InitializeComponent faria, 
        // mas de forma explícita para evitar erros de compilação.
        AvaloniaXamlLoader.Load(this);
    }
}