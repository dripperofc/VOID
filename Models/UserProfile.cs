using CommunityToolkit.Mvvm.ComponentModel;

namespace Void.Models;

public partial class UserProfile : ObservableObject
{
    [ObservableProperty] private string _name = "Dono do Void";
    [ObservableProperty] private string _color = "#FF3BF1";
    [ObservableProperty] private string _badge = "👑";
    [ObservableProperty] private string _status = "Online";
}