using System;
using Tesseract;

namespace QBScannerBridge.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;
        private readonly object _lock = new object();
        private bool _disposed;

        public OcrService(string tessDataPath)
        {
            _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
        }

        public string ExtractTextFromImage(string imagePath)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OcrService));

            lock (_lock)
            {
                using (var img = Pix.LoadFromFile(imagePath))
                using (var page = _engine.Process(img))
                {
                    return page.GetText();
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _engine?.Dispose();
                _disposed = true;
            }
        }
    }
}
