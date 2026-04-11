using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Void.Services;

public class ChatService
{
    private readonly HubConnection _connection;
    
    // Esse evento avisa o ViewModel quando uma mensagem nova chega da internet
    public event Action<string, string, string, string>? OnMessageReceived;

    public ChatService()
    {
        // AQUI ESTÁ O SEU LINK EXATO!
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5159/chathub") 
            .Build();

        // Fica escutando o servidor... se chegar algo, ele dispara o evento
        _connection.On<string, string, string, string>("ReceiveMessage", (user, message, color, badge) =>
        {
            OnMessageReceived?.Invoke(user, message, color, badge);
        });
    }

    public async Task ConnectAsync()
    {
        try
        {
            await _connection.StartAsync();
            Console.WriteLine("[VOID NETWORK] Conectado ao Servidor Central!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VOID NETWORK] Erro ao conectar: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(string user, string message, string color, string badge)
    {
        try
        {
            await _connection.InvokeAsync("SendMessage", user, message, color, badge);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VOID NETWORK] Erro ao enviar mensagem: {ex.Message}");
        }
    }
}