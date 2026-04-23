using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using QBScannerBridge.Helpers;
using QBScannerBridge.Models;
using QBScannerBridge.Services;

namespace QBScannerBridge
{
    public partial class MainWindow : Window
    {
        private const string WatchFolder = @"C:\Scans\QB";

        private readonly FolderWatcherService _watcher;
        private readonly PdfToImageService _pdfToImage;
        private readonly OcrService _ocr;
        private readonly ParsingService _parser;
        private readonly QuickBooksService _qb;

        private readonly ObservableCollection<ScanDocument> _docs = new ObservableCollection<ScanDocument>();

        public MainWindow()
        {
            InitializeComponent();

            DocsList.ItemsSource = _docs;

            _watcher = new FolderWatcherService(WatchFolder);
            _watcher.FileDetected += OnFileDetected;

            _pdfToImage = new PdfToImageService();

            // tessdata folder next to exe
            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            _ocr = new OcrService(tessDataPath);

            _parser = new ParsingService();
            _qb = new QuickBooksService();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _watcher.Start();
            MessageBox.Show("Watching started. Drop PDFs/images into C:\\Scans\\QB.");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _watcher.Stop();
            MessageBox.Show("Watching stopped.");
        }

        private void DocsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DocsList.SelectedItem is ScanDocument doc)
                LoadDocToUI(doc);
        }

        private async void OnFileDetected(string path)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var doc = new ScanDocument
                {
                    FilePath = path,
                    FileName = System.IO.Path.GetFileName(path),
                    Status = "Detected"
                };

                _docs.Insert(0, doc);
                DocsList.SelectedItem = doc;
            });

            await ProcessDocumentAsync(path);
        }

        private async Task ProcessDocumentAsync(string path)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var doc = FindDoc(path);
                if (doc != null) doc.Status = "Waiting for file...";
                DocsList.Items.Refresh();
            });

            if (!SafeFileHelper.WaitUntilFileReady(path))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var doc = FindDoc(path);
                    if (doc != null) doc.Status = "File not ready / locked";
                    DocsList.Items.Refresh();
                });
                return;
            }

            string tempImage = null;

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var doc = FindDoc(path);
                    if (doc != null) doc.Status = "OCR...";
                    DocsList.Items.Refresh();
                });

                string ocrText;

                if (SafeFileHelper.IsPdf(path))
                {
                    tempImage = _pdfToImage.RenderFirstPageToTempPng(path);
                    ocrText = _ocr.ExtractTextFromImage(tempImage);
                }
                else
                {
                    ocrText = _ocr.ExtractTextFromImage(path);
                }

                var parsed = _parser.Parse(path, ocrText);
                parsed.Status = "Ready to review";

                await Dispatcher.InvokeAsync(() =>
                {
                    ReplaceDoc(path, parsed);
                    DocsList.Items.Refresh();

                    if (DocsList.SelectedItem is ScanDocument selected &&
                        string.Equals(selected.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        LoadDocToUI(parsed);
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var doc = FindDoc(path);
                    if (doc != null) doc.Status = "Error: " + ex.Message;
                    DocsList.Items.Refresh();
                });
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempImage))
                {
                    try { File.Delete(tempImage); } catch { }
                }
            }
        }

        private void BtnPost_Click(object sender, RoutedEventArgs e)
        {
            if (!(DocsList.SelectedItem is ScanDocument doc))
                return;

            doc.VendorName = TxtVendor.Text?.Trim();
            doc.InvoiceNumber = TxtInvoice.Text?.Trim();
            doc.ExpenseAccount = TxtAccount.Text?.Trim();
            doc.Memo = TxtMemo.Text?.Trim();

            if (DateTime.TryParse(TxtDate.Text, out var dt))
                doc.InvoiceDate = dt;
            else
                doc.InvoiceDate = null;

            if (decimal.TryParse(TxtTotal.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
                doc.TotalAmount = total;
            else if (decimal.TryParse(TxtTotal.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out total))
                doc.TotalAmount = total;
            else
                doc.TotalAmount = null;

            try
            {
                doc.Status = "Posting to QuickBooks...";
                DocsList.Items.Refresh();

                string resp = _qb.AddBill(doc);

                doc.Status = "Posted";
                TxtStatus.Text = doc.Status;
                TxtResponse.Text = (resp?.Length > 2000) ? resp.Substring(0, 2000) : resp;
                DocsList.Items.Refresh();
            }
            catch (Exception ex)
            {
                doc.Status = "Post failed: " + ex.Message;
                TxtStatus.Text = doc.Status;
                DocsList.Items.Refresh();
                MessageBox.Show(ex.ToString(), "QuickBooks post failed");
            }
        }

        private void LoadDocToUI(ScanDocument doc)
        {
            TxtVendor.Text = doc.VendorName;
            TxtInvoice.Text = doc.InvoiceNumber;
            TxtDate.Text = doc.InvoiceDate?.ToString("yyyy-MM-dd") ?? "";
            TxtTotal.Text = doc.TotalAmount?.ToString("0.00") ?? "";
            TxtAccount.Text = doc.ExpenseAccount;
            TxtMemo.Text = doc.Memo;
            TxtStatus.Text = doc.Status;
            TxtRaw.Text = doc.RawText;
        }

        private ScanDocument FindDoc(string path)
        {
            foreach (var d in _docs)
            {
                if (string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    return d;
            }
            return null;
        }

        private void ReplaceDoc(string path, ScanDocument newDoc)
        {
            for (int i = 0; i < _docs.Count; i++)
            {
                if (string.Equals(_docs[i].FilePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    _docs[i] = newDoc;
                    return;
                }
            }

            _docs.Insert(0, newDoc);
        }
    }
}
