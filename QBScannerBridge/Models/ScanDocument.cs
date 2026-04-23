using System;

namespace QBScannerBridge.Models
{
    public class ScanDocument
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string RawText { get; set; }
        public string VendorName { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string ExpenseAccount { get; set; } = "Parts Expense";
        public string Memo { get; set; } = "Imported from scanned document";
        public string Status { get; set; } = "Waiting";

        public override string ToString() => $"{FileName} ({Status})";
    }
}
