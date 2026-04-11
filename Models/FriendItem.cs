using CommunityToolkit.Mvvm.ComponentModel;

namespace Void.Models;

public partial class FriendItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _status = "Online";
}