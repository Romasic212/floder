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
            try
            {
                await _socket.ConnectAsync(new Uri(serverUrl), CancellationToken.None);
                OnMessage?.Invoke("✅ Подключено к серверу");

                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                OnMessage?.Invoke("❌ Ошибка подключения: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            while (_socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                OnMessage?.Invoke("📥 " + message);
            }
        }

        public async Task Send(string message)
        {
            if (_socket.State != WebSocketState.Open)
            {
                OnMessage?.Invoke("❌ Нет подключения к серверу");
                return;
            }

            try
            {
                var data = Encoding.UTF8.GetBytes(message);

                await _socket.SendAsync(
                    data,
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                OnMessage?.Invoke("📤 Отправлено");
            }
            catch (Exception ex)
            {
                OnMessage?.Invoke("❌ Ошибка отправки: " + ex.Message);
            }
        }
    }
}