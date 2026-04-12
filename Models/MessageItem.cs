using System;

namespace Void.Models;

public class MessageItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public UserProfile Author { get; set; } = new();
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public DateTime? EditedAt { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    
    public string TimeDisplay => Timestamp.ToString("HH:mm");
}