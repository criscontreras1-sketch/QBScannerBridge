using Tesseract;

namespace QBScannerBridge.Services
{
    public class OcrService
    {
        private readonly string _tessDataPath;

        public OcrService(string tessDataPath)
        {
            _tessDataPath = tessDataPath;
        }

        public string ExtractTextFromImage(string imagePath)
        {
            using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
            using (var img = Pix.LoadFromFile(imagePath))
            using (var page = engine.Process(img))
            {
                return page.GetText();
            }
        }
    }
}
