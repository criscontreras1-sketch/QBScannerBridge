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
        private FolderWatcherService _watcher;
        private readonly PdfToImageService _pdfToImage;
        private readonly OcrService _ocr;
        private readonly ParsingService _parser;
        private readonly QuickBooksService _qb;

        private readonly ObservableCollection<ScanDocument> _docs = new ObservableCollection<ScanDocument>();

        public MainWindow()
        {
            InitializeComponent();

            DocsList.ItemsSource = _docs;

            _pdfToImage = new PdfToImageService();

            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            _ocr = new OcrService(tessDataPath);

            _parser = new ParsingService();
            _qb = new QuickBooksService();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string folder = TxtWatchFolder.Text?.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                SetFooterStatus("Please enter a watch folder path.");
                return;
            }

            _watcher?.Stop();
            _watcher?.Dispose();

            _watcher = new FolderWatcherService(folder);
            _watcher.FileDetected += OnFileDetected;
            _watcher.Start();

            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            SetFooterStatus($"Watching: {folder}");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _watcher?.Stop();
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            SetFooterStatus("Watching stopped.");
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
                    FileName = Path.GetFileName(path),
                    Status = "Detected"
                };

                _docs.Insert(0, doc);
                DocsList.SelectedItem = doc;
                SetFooterStatus($"File detected: {doc.FileName}");
            });

            await ProcessDocumentAsync(path);
        }

        private async Task ProcessDocumentAsync(string path)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var doc = FindDoc(path);
                if (doc != null) doc.Status = "Waiting for file\u2026";
            });

            if (!await SafeFileHelper.WaitUntilFileReadyAsync(path))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var doc = FindDoc(path);
                    if (doc != null) doc.Status = "File locked / not ready";
                    SetFooterStatus($"Could not read: {Path.GetFileName(path)}");
                });
                return;
            }

            string tempImage = null;

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var doc = FindDoc(path);
                    if (doc != null) doc.Status = "Running OCR\u2026";
                    SetFooterStatus($"OCR: {Path.GetFileName(path)}");
                });

                string ocrText = await Task.Run(() =>
                {
                    if (SafeFileHelper.IsPdf(path))
                    {
                        tempImage = _pdfToImage.RenderFirstPageToTempPng(path);
                        return _ocr.ExtractTextFromImage(tempImage);
                    }
                    return _ocr.ExtractTextFromImage(path);
                });

                var parsed = _parser.Parse(path, ocrText);
                parsed.Status = "Ready to review";

                await Dispatcher.InvokeAsync(() =>
                {
                    ReplaceDoc(path, parsed);

                    if (DocsList.SelectedItem is ScanDocument selected &&
                        string.Equals(selected.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        LoadDocToUI(parsed);
                    }

                    SetFooterStatus($"Ready: {parsed.FileName}");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var doc = FindDoc(path);
                    if (doc != null) doc.Status = "Error: " + ex.Message;
                    SetFooterStatus($"Error processing {Path.GetFileName(path)}: {ex.Message}");
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

        private async void BtnPost_Click(object sender, RoutedEventArgs e)
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

            if (string.IsNullOrWhiteSpace(doc.VendorName))
            {
                SetFooterStatus("Vendor name is required.");
                return;
            }
            if (!doc.InvoiceDate.HasValue)
            {
                SetFooterStatus("Invoice date is required.");
                return;
            }
            if (!doc.TotalAmount.HasValue)
            {
                SetFooterStatus("Total amount is required.");
                return;
            }

            BtnPost.IsEnabled = false;
            doc.Status = "Posting to QuickBooks\u2026";
            TxtStatus.Text = doc.Status;
            SetFooterStatus("Posting to QuickBooks\u2026");

            try
            {
                string resp = await Task.Run(() => _qb.AddBill(doc));

                doc.Status = "Posted";
                doc.LastResponse = resp;
                TxtStatus.Text = doc.Status;
                TxtResponse.Text = resp;
                SetFooterStatus($"Posted bill for {doc.VendorName}.");
            }
            catch (Exception ex)
            {
                doc.Status = "Post failed";
                doc.LastResponse = ex.Message;
                TxtStatus.Text = doc.Status;
                TxtResponse.Text = ex.Message;
                SetFooterStatus("Post failed: " + ex.Message);
                MessageBox.Show(ex.ToString(), "QuickBooks Post Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnPost.IsEnabled = true;
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
            TxtResponse.Text = doc.LastResponse ?? string.Empty;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _watcher?.Stop();
            _watcher?.Dispose();
            _ocr?.Dispose();
        }

        private void SetFooterStatus(string message)
        {
            TxtFooterStatus.Text = message;
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
