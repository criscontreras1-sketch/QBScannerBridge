using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QBScannerBridge.Models
{
    public class ScanDocument : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private string _filePath;
        public string FilePath { get => _filePath; set => Set(ref _filePath, value); }

        private string _fileName;
        public string FileName { get => _fileName; set => Set(ref _fileName, value); }

        private string _rawText;
        public string RawText { get => _rawText; set => Set(ref _rawText, value); }

        private string _vendorName;
        public string VendorName { get => _vendorName; set => Set(ref _vendorName, value); }

        private string _invoiceNumber;
        public string InvoiceNumber { get => _invoiceNumber; set => Set(ref _invoiceNumber, value); }

        private DateTime? _invoiceDate;
        public DateTime? InvoiceDate { get => _invoiceDate; set => Set(ref _invoiceDate, value); }

        private decimal? _totalAmount;
        public decimal? TotalAmount { get => _totalAmount; set => Set(ref _totalAmount, value); }

        private string _expenseAccount = "Parts Expense";
        public string ExpenseAccount { get => _expenseAccount; set => Set(ref _expenseAccount, value); }

        private string _memo = "Imported from scanned document";
        public string Memo { get => _memo; set => Set(ref _memo, value); }

        private string _status = "Waiting";
        public string Status { get => _status; set => Set(ref _status, value); }

        private string _lastResponse;
        public string LastResponse { get => _lastResponse; set => Set(ref _lastResponse, value); }

        public override string ToString() => $"{FileName} ({Status})";
    }
}
