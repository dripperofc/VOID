using System.Collections.ObjectModel;

namespace Void.Models;

public class ServerItem
{
    public string Name { get; set; } = "";
    public string Initial { get; set; } = "";
    public int OwnerId { get; set; } = 0; // ID do criador do servidor
    public ObservableCollection<string> Channels { get; set; } = new();
}