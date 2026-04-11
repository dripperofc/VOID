using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Void.Models;

public partial class ServerItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    public string Initial => !string.IsNullOrEmpty(Name) ? Name[0].ToString().ToUpper() : "?";
    public ObservableCollection<string> Channels { get; } = new();
}