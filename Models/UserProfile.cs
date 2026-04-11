using CommunityToolkit.Mvvm.ComponentModel;

namespace Void.Models;

public partial class UserProfile : ObservableObject
{
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _nickname = "";
    [ObservableProperty] private int _userId;
    [ObservableProperty] private string _badge = "";
    [ObservableProperty] private string _color = "#3498db"; // Azul padrão do Void
}