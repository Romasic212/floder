using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private WebSocketService _ws = new WebSocketService();

        private List<FileMeta> _currentFiles;

        public MainWindow()
        {
            InitializeComponent();

            _watcher.OnChanged += msg =>
                Dispatcher.Invoke(() => FilesList.Items.Add(msg));

            _ws.OnMessage += msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    FilesList.Items.Add("Получено: " + msg);
                });
            };
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            await _ws.Connect("ws://localhost:5000/ws");
            FilesList.Items.Add("Подключено к серверу");
        }

        private async void BtnSendIndex_Click(object sender, RoutedEventArgs e)
        {
            var json = JsonSerializer.Serialize(_currentFiles);
            await _ws.Send(json);
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

                _currentFiles = _indexer.Scan(folderPath);

                FilesList.ItemsSource = _currentFiles.Select(f => f.Path).ToList();

                _watcher.Start(folderPath);
            }
        }
    }
}