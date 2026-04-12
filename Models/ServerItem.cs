using System.Collections.Generic;

namespace Void.Models;

public class ServerItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int OwnerId { get; set; }
    public bool IsOfficial { get; set; }
    public List<ChannelItem> Channels { get; set; } = new();
    public List<int> MemberIds { get; set; } = new();
}

public class ChannelItem
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public ChannelType Type { get; set; }
    public int Position { get; set; }
}

public enum ChannelType
{
    Text,
    Voice,
    Announcement
}

// NÃO COLOQUE FriendItem aqui!