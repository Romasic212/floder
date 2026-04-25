using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Microsoft.Win32;
using floder.Core;
using floder.Models;
using floder.Network;

namespace floder
{
    public partial class MainWindow : Window
    {
        private Indexer _indexer = new Indexer();
        private FolderWatcher _watcher = new FolderWatcher();
        private TcpService _tcp = new TcpService();
        private UdpDiscovery _udp = new UdpDiscovery();

        private List<FileMeta> _currentFiles;
        private string _currentFolder;

        private Dictionary<string, string> _devices = new();
        private string _myIp;

        public MainWindow()
        {
            InitializeComponent();

            _myIp = GetLocalIP();
            TxtMyIP.Text = "Мой IP: " + _myIp;

            _watcher.OnChanged += msg =>
                Dispatcher.Invoke(() => FilesList.Items.Add(msg));

            _tcp.OnMessageReceived += msg =>
                Dispatcher.Invoke(() => FilesList.Items.Add(msg));

            _tcp.OnIndexReceived += async remoteFiles =>
            {
                var toSend = _currentFiles
                    .Where(local => !remoteFiles.Any(r => r.Path == local.Path))
                    .ToList();

                foreach (var file in toSend)
                {
                    var ip = GetSelectedIP();
                    if (ip != null)
                        await _tcp.SendFile(ip, _currentFolder, file.Path);
                }
            };

            _udp.OnDeviceFound += (ip, name) =>
            {
                if (ip == _myIp) return;

                Dispatcher.Invoke(() =>
                {
                    if (!_devices.ContainsKey(ip))
                    {
                        _devices[ip] = name;
                        DevicesList.Items.Add($"{name} ({ip})");
                    }
                });
            };

            _tcp.StartServer();
            _udp.StartListening();
        }

        private string GetSelectedIP()
        {
            if (DevicesList.SelectedItem == null) return null;

            var selected = DevicesList.SelectedItem.ToString();
            return _devices.First(d => selected.Contains(d.Key)).Key;
        }

        // 🔥 РУЧНОЕ ПОДКЛЮЧЕНИЕ
        private async void BtnConnectManual_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFiles == null || string.IsNullOrWhiteSpace(TxtIP.Text))
                return;

            await _tcp.SendIndex(TxtIP.Text, _currentFiles);
        }

        private async void BtnFindDevices_Click(object sender, RoutedEventArgs e)
        {
            DevicesList.Items.Clear();
            _devices.Clear();

            await _udp.Broadcast();
        }

        private async void BtnConnectSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFiles == null) return;

            var ip = GetSelectedIP();
            if (ip != null)
                await _tcp.SendIndex(ip, _currentFiles);
        }

        private string GetLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }

            return "Не найден";
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Выберите папку"
            };

            if (dialog.ShowDialog() == true)
            {
                var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);

                _currentFolder = folderPath;

                TxtFolder.Text = folderPath;

                _currentFiles = _indexer.Scan(folderPath);

                FilesList.ItemsSource = _currentFiles.Select(f => f.Path).ToList();

                _watcher.Start(folderPath);
            }
        }
    }
}