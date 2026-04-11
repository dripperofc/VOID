namespace Void.Models;

public class MessageItem
{
    public string Author { get; set; } = "";
    public string Content { get; set; } = "";
    public string NameColor { get; set; } = "#FFFFFF";
    public string Badge { get; set; } = "";
    public string Timestamp { get; set; } = "";

    // NOVO
    public bool ShowAuthor { get; set; } = true;
}