using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace floder.Network
{
    public class UdpDiscovery
    {
        private UdpClient _udp;

        public event Action<string, string> OnDeviceFound;

        public void StartListening(int port = 9001)
        {
            _udp = new UdpClient(port);

            Task.Run(async () =>
            {
                while (true)
                {
                    var result = await _udp.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    try
                    {
                        var data = JsonSerializer.Deserialize<DeviceInfo>(message);

                        if (data != null)
                        {
                            OnDeviceFound?.Invoke(result.RemoteEndPoint.Address.ToString(), data.Name);
                        }
                    }
                    catch { }
                }
            });
        }

        public async Task Broadcast(int port = 9001)
        {
            var udp = new UdpClient();
            udp.EnableBroadcast = true;

            var data = new DeviceInfo
            {
                Name = Environment.MachineName
            };

            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);

            await udp.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, port));
        }

        private class DeviceInfo
        {
            public string Name { get; set; }
        }
    }
}