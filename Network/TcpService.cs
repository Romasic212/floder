using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using floder.Models;

namespace floder.Network
{
    public class TcpService
    {
        private TcpListener _listener;

        public event Action<string> OnMessageReceived;
        public event Action<List<FileMeta>> OnIndexReceived;

        public void StartServer(int port = 9000)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    HandleClient(client);
                }
            });
        }

        private async void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();

            int type = stream.ReadByte();

            if (type == 1)
                await HandleIndex(stream);
            else if (type == 2)
                await HandleFile(stream);
        }

        private async Task HandleIndex(NetworkStream stream)
        {
            var buffer = new byte[8192];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            var files = JsonSerializer.Deserialize<List<FileMeta>>(json);

            if (files != null)
            {
                OnIndexReceived?.Invoke(files);
                OnMessageReceived?.Invoke($"Получен индекс: {files.Count} файлов");
            }
        }

        private async Task HandleFile(NetworkStream stream)
        {
            var reader = new BinaryReader(stream);

            string path = reader.ReadString();
            long size = reader.ReadInt64();

            string fullPath = Path.Combine(Environment.CurrentDirectory, "Synced", path);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var file = File.Create(fullPath);

            var buffer = new byte[8192];
            long remaining = size;

            while (remaining > 0)
            {
                int read = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0) break;

                await file.WriteAsync(buffer, 0, read);
                remaining -= read;
            }

            OnMessageReceived?.Invoke($"Получен файл: {path}");
        }

        public async Task SendIndex(string ip, List<FileMeta> files)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ip), 9000);

            var stream = client.GetStream();

            stream.WriteByte(1);

            var json = JsonSerializer.Serialize(files);
            var data = Encoding.UTF8.GetBytes(json);

            await stream.WriteAsync(data, 0, data.Length);
        }

        public async Task SendFile(string ip, string rootFolder, string filePath)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ip), 9000);

            var stream = client.GetStream();

            stream.WriteByte(2);

            var writer = new BinaryWriter(stream);

            var relativePath = Path.GetRelativePath(rootFolder, filePath);
            var info = new FileInfo(filePath);

            writer.Write(relativePath);
            writer.Write(info.Length);

            using var file = File.OpenRead(filePath);
            await file.CopyToAsync(stream);

            OnMessageReceived?.Invoke($"Отправлен файл: {relativePath}");
        }
    }
}