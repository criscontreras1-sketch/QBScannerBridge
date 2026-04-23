using System;
using System.IO;

namespace QBScannerBridge.Services
{
    public class FolderWatcherService
    {
        private readonly FileSystemWatcher _watcher;
        public event Action<string> FileDetected;

        public FolderWatcherService(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            _watcher = new FileSystemWatcher(folderPath)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = false
            };

            _watcher.Created += Watcher_Created;
        }

        public void Start() => _watcher.EnableRaisingEvents = true;
        public void Stop() => _watcher.EnableRaisingEvents = false;

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath)?.ToLowerInvariant();
            if (ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tif" || ext == ".tiff" || ext == ".bmp")
            {
                FileDetected?.Invoke(e.FullPath);
            }
        }
    }
}
