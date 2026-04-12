using System.ComponentModel;

namespace Void.Models;

public class FriendItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify(string prop) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public int Id { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public string AvatarColor { get; set; } = "#5865F2";

    // Status com notificação — assim a UI atualiza quando muda online/offline
    private string _status = "Offline";
    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            Notify(nameof(Status));
            Notify(nameof(IsOnline));
            Notify(nameof(StatusColor));
        }
    }

    public bool IsOnline => Status == "Online";

    // Cor da bolinha de status
    public string StatusColor => Status switch
    {
        "Online"        => "#43B581",
        "Ausente"       => "#FAA61A",
        "Não Perturbe"  => "#F04747",
        _               => "#747F8D"  // Offline
    };
}