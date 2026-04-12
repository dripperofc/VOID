// FIX #5: removido AvaloniaXamlLoader.Load(this) manual — o Avalonia 11+ gera
// InitializeComponent() automaticamente via source generators.
// FIX AVISO #5: removidos usings desnecessários (Avalonia.Input, Avalonia.Interactivity)

using Avalonia.Controls;

namespace Void.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}