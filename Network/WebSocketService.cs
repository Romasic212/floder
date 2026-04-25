using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace floder.Network
{
    public class WebSocketService
    {
        private ClientWebSocket _socket = new ClientWebSocket();

        public event Action<string> OnMessage;

        public async Task Connect(string serverUrl)
        {
            await _socket.ConnectAsync(new Uri(serverUrl), CancellationToken.None);

            _ = ReceiveLoop();
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            while (_socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                OnMessage?.Invoke(message);
            }
        }

        public async Task Send(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);

            await _socket.SendAsync(
                data,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
}