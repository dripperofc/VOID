using System.Collections.ObjectModel;

namespace Void.Models;

public class ServerItem
{
    public string Name { get; set; } = "";
    public string Initial { get; set; } = "";
    public ObservableCollection<string> Channels { get; set; } = new();
}