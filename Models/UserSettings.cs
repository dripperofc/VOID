namespace Void.Models;

public class UserSettings
{
    public string Nickname { get; set; } = ""; // Apelido que aparece no chat
    public string Username { get; set; } = ""; // @nome_unico
    public string Password { get; set; } = "";
    public string Color { get; set; } = "#5865F2";
    public string Badge { get; set; } = "";
    public int UserId { get; set; } = 0;
}