using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Void.Models;

public partial class MessageItem : ObservableObject
{
    [ObservableProperty] private string _author = "";
    [ObservableProperty] private string _content = "";
    [ObservableProperty] private string _timestamp = DateTime.Now.ToString("HH:mm");
    [ObservableProperty] private string _nameColor = "#FFFFFF";
    [ObservableProperty] private string _badge = "";
}