using Avalonia.Controls;
using Void.ViewModels;

namespace Void.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(); // Conecta o motor à tela
    }
}