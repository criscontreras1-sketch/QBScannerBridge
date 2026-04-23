using System;
using System.IO;
using PdfiumViewer;

namespace QBScannerBridge.Services
{
    public class PdfToImageService
    {
        public string RenderFirstPageToTempPng(string pdfPath)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

            using (var document = PdfDocument.Load(pdfPath))
            using (var image = document.Render(0, 220, 220, true))
            {
                image.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            return tempPath;
        }
    }
}
