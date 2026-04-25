using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var clients = new List<WebSocket>();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        clients.Add(socket);

        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // пересылаем всем остальным
            foreach (var client in clients)
            {
                if (client != socket && client.State == WebSocketState.Open)
                {
                    await client.SendAsync(
                        Encoding.UTF8.GetBytes(message),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }

        clients.Remove(socket);
    }
});

app.Run("http://0.0.0.0:5000");