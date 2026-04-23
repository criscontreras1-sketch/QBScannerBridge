using System.IO;
using System.Threading;

namespace QBScannerBridge.Helpers
{
    public static class SafeFileHelper
    {
        public static bool WaitUntilFileReady(string path, int maxAttempts = 20, int delayMs = 500)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        return true;
                    }
                }
                catch
                {
                    Thread.Sleep(delayMs);
                }
            }

            return false;
        }

        public static bool IsImage(string path)
        {
            string ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tif" || ext == ".tiff" || ext == ".bmp";
        }

        public static bool IsPdf(string path)
        {
            string ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext == ".pdf";
        }
    }
}
