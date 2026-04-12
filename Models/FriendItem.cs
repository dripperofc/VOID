namespace Void.Models;

public class FriendItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Offline";
    public string Initials { get; set; } = "?";
    public string AvatarColor { get; set; } = "#5865F2";
    public bool IsOnline => Status == "Online";
}