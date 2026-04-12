using System;

namespace Void.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string AvatarColor { get; set; } = "#5865F2";
    public string Initials { get; set; } = "?";
    public bool IsOnline { get; set; } = true;
    public bool IsOwner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastLogin { get; set; } = DateTime.Now;
    
    public string GetDisplayName() => string.IsNullOrWhiteSpace(Nickname) ? Username : Nickname;
}