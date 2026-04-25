using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Void.Models;

namespace Void.Services;

// Handler que injeta o header ngrok em TODAS as requisições HTTP (negotiate + WebSocket upgrade)
internal class NgrokHeaderHandler : DelegatingHandler
{
    public NgrokHeaderHandler(HttpMessageHandler inner) : base(inner) { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        return base.SendAsync(request, ct);
    }
}

public class ChatService
{
    // ============================================================
    // TODA VEZ QUE REINICIAR O NGROK, COLE O LINK NOVO AQUI:
    // Exemplo: "https://a1b2-c3d4.ngrok-free.app"
    private const string ServerUrl = "https://unretrogressively-standardizable-dung.ngrok-free.dev";
    // ============================================================

    private HubConnection? _connection;

    public event Action<MessageItem>? MessageReceived;
    public event Action<MessageItem>? PrivateMessageReceived;
    public event Action<string, bool>? OnUserStatusChanged;
    public event Action<string>? FriendRequestReceived;
    public event Action<string>? FriendRequestFailed;
    public event Action<string>? FriendRequestSent;
    public event Action<string>? FriendAccepted;
    public event Action<string>? ConnectionFailed;

    // ── Chamadas de Voz ───────────────────────────────────────────────────
    private VoiceCallService? _voiceCallService;
    /// <summary>Disponível após ConnectAsync. Gerencia chamadas de voz.</summary>
    public VoiceCallService? Voice => _voiceCallService;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string username)
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{ServerUrl}/voidchat?username={username}", options =>
            {
                // Header no options (cobre parte da negociação)
                options.Headers["ngrok-skip-browser-warning"] = "true";

                // Handler HTTP garante o header em TODAS as requisições (negotiate + polling)
                options.HttpMessageHandlerFactory = inner =>
                    new NgrokHeaderHandler(inner);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<object>("ReceiveMessage", raw =>
        {
            var msg = ParseMsg(raw);
            if (msg != null) MessageReceived?.Invoke(msg);
        });

        _connection.On<object, string>("ReceivePrivateMessage", (raw, from) =>
        {
            var msg = ParseMsg(raw);
            if (msg != null) PrivateMessageReceived?.Invoke(msg);
        });

        _connection.On<string, bool>("UserStatusChanged", (u, online) =>
            OnUserStatusChanged?.Invoke(u, online));

        _connection.On<string>("ReceiveFriendRequest", from =>
            FriendRequestReceived?.Invoke(from));

        _connection.On<string>("FriendRequestFailed", reason =>
            FriendRequestFailed?.Invoke(reason));

        _connection.On<string>("FriendRequestSent", to =>
            FriendRequestSent?.Invoke(to));

        _connection.On<string>("FriendAccepted", friend =>
            FriendAccepted?.Invoke(friend));

        try
        {
            await _connection.StartAsync();
            _voiceCallService = new VoiceCallService(new LoggingService(), _connection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Conexao falhou: {ex.Message}");
            ConnectionFailed?.Invoke(ex.Message);
        }
    }

    public async Task<string> AuthenticateAsync(string username, string password, bool isRegister)
    {
        if (!IsConnected) await ConnectAsync(username);
        if (_connection == null) return "error";
        try
        {
            return await _connection.InvokeAsync<string>("AuthenticateUser", username, password, isRegister);
        }
        catch (Exception ex) { Console.WriteLine($"ERRO: {ex.Message}"); return "error"; }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        if (!IsConnected || _connection == null) return null;
        try
        {
            var raw = await _connection.InvokeAsync<object?>("GetUserProfile", username);
            if (raw == null) return null;
            var json = JsonSerializer.Serialize(raw);
            return JsonSerializer.Deserialize<UserProfileDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    public async Task<List<string>> GetPendingRequestsAsync(string username)
    {
        if (!IsConnected || _connection == null) return new();
        try { return await _connection.InvokeAsync<List<string>>("GetPendingRequests", username); }
        catch { return new(); }
    }

    public async Task SendFriendRequestAsync(string targetUsername)
    {
        if (IsConnected) await _connection!.InvokeAsync("SendFriendRequest", targetUsername);
    }

    public async Task AcceptFriendRequestAsync(string requesterUsername)
    {
        if (IsConnected) await _connection!.InvokeAsync("AcceptFriendRequest", requesterUsername);
    }

    public async Task DeclineFriendRequestAsync(string requesterUsername)
    {
        if (IsConnected) await _connection!.InvokeAsync("DeclineFriendRequest", requesterUsername);
    }

    public async Task SendPrivateMessageAsync(string fromNickname, string targetUsername, string content)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("SendPrivateMessage", fromNickname, targetUsername, content);
    }

    public async Task<List<MessageItem>> GetChatHistoryAsync(string targetUsername)
    {
        if (!IsConnected || _connection == null) return new();
        try
        {
            var rawList = await _connection.InvokeAsync<List<object>>("GetChatHistory", targetUsername);
            var result = new List<MessageItem>();
            foreach (var raw in rawList)
            {
                var msg = ParseMsg(raw);
                if (msg != null) result.Add(msg);
            }
            return result;
        }
        catch { return new(); }
    }

    public async Task JoinChannelAsync(string serverId, string channelId)
    {
        if (IsConnected) await _connection!.InvokeAsync("JoinChannel", serverId, channelId);
    }

    public async Task SendMessageAsync(MessageItem message, string serverId, string channelId)
    {
        if (IsConnected)
            await _connection!.InvokeAsync("SendMessage",
                message.Author?.Nickname ?? "?", message.Content, "#FFF", "", serverId, channelId);
    }

    public void SimulateMessageReceived(MessageItem msg) => MessageReceived?.Invoke(msg);

    public async Task DisconnectAsync()
    {
        if (_connection != null) { await _connection.DisposeAsync(); _connection = null; }
    }

    private static MessageItem? ParseMsg(object raw)
    {
        try
        {
            var json = raw is JsonElement je ? je.GetRawText() : JsonSerializer.Serialize(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var nickname = "";
            var initials = "?";
            if (root.TryGetProperty("author", out var authorEl) || root.TryGetProperty("Author", out authorEl))
            {
                if (authorEl.TryGetProperty("nickname", out var n) || authorEl.TryGetProperty("Nickname", out n))
                    nickname = n.GetString() ?? "";
                if (authorEl.TryGetProperty("initials", out var i) || authorEl.TryGetProperty("Initials", out i))
                    initials = i.GetString() ?? "?";
            }

            var content = "";
            if (root.TryGetProperty("content", out var c) || root.TryGetProperty("Content", out c))
                content = c.GetString() ?? "";

            DateTime ts = DateTime.Now;
            if (root.TryGetProperty("timestamp", out var t) || root.TryGetProperty("Timestamp", out t))
                if (t.TryGetDateTime(out var dt)) ts = dt.ToLocalTime();

            return new MessageItem
            {
                Content   = content,
                Timestamp = ts,
                Author = new UserProfile { Nickname = nickname, Initials = initials }
            };
        }
        catch { return null; }
    }
}

public class UserProfileDto
{
    public string   Username    { get; set; } = "";
    public string   Nickname    { get; set; } = "";
    public string   AvatarColor { get; set; } = "#5865F2";
    public string   Initials    { get; set; } = "?";
    public string[] Friends     { get; set; } = Array.Empty<string>();
    // FIX: campo de presença — hidrata o status dos amigos no login
    public bool     IsOnline    { get; set; }
}
