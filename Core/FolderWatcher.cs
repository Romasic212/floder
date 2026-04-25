using System;
using System.IO;

namespace floder.Core
{
    public class FolderWatcher
    {
        private FileSystemWatcher _watcher;

        public event Action<string> OnChanged;

        public void Start(string path)
        {
            _watcher = new FileSystemWatcher(path);

            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;

            _watcher.Created += (s, e) => OnChanged?.Invoke($"Создан: {e.FullPath}");
            _watcher.Deleted += (s, e) => OnChanged?.Invoke($"Удалён: {e.FullPath}");
            _watcher.Changed += (s, e) => OnChanged?.Invoke($"Изменён: {e.FullPath}");
            _watcher.Renamed += (s, e) => OnChanged?.Invoke($"Переименован: {e.FullPath}");
        }
    }
}