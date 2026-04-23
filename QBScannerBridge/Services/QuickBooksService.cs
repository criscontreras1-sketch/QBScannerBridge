using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using QBScannerBridge.Models;
using QBXMLRP2Lib;

namespace QBScannerBridge.Services
{
    public class QuickBooksService
    {
        public string AddBill(ScanDocument doc)
        {
            if (string.IsNullOrWhiteSpace(doc.VendorName))
                throw new InvalidOperationException("Vendor name is required.");

            if (!doc.InvoiceDate.HasValue)
                throw new InvalidOperationException("Invoice date is required.");

            if (!doc.TotalAmount.HasValue)
                throw new InvalidOperationException("Total amount is required.");

            RequestProcessor2 rp = null;
            string ticket = null;

            try
            {
                string qbxml = BuildBillAddRequest(doc);

                rp = new RequestProcessor2();
                rp.OpenConnection2("", "QBScannerBridge", QBXMLRPConnectionType.localQBD);
                ticket = rp.BeginSession("", QBFileMode.qbFileOpenDoNotCare);

                string response = rp.ProcessRequest(ticket, qbxml);
                return response;
            }
            finally
            {
                if (rp != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(ticket))
                            rp.EndSession(ticket);
                    }
                    catch { }

                    try { rp.CloseConnection(); } catch { }

                    try { Marshal.ReleaseComObject(rp); } catch { }
                }
            }
        }

        private string BuildBillAddRequest(ScanDocument doc)
        {
            string vendor = XmlEscape(doc.VendorName);
            string refNumber = XmlEscape(doc.InvoiceNumber ?? string.Empty);
            string account = XmlEscape(doc.ExpenseAccount ?? "Parts Expense");
            string memo = XmlEscape(doc.Memo ?? "Imported from scanned document");
            string txnDate = doc.InvoiceDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string amount = doc.TotalAmount.Value.ToString("0.00", CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<?qbxml version=\"13.0\"?>");
            sb.Append("<QBXML>");
            sb.Append("<QBXMLMsgsRq onError=\"stopOnError\">");
            sb.Append("<BillAddRq>");
            sb.Append("<BillAdd>");
            sb.Append($"<VendorRef><FullName>{vendor}</FullName></VendorRef>");
            sb.Append($"<TxnDate>{txnDate}</TxnDate>");
            if (!string.IsNullOrWhiteSpace(refNumber))
                sb.Append($"<RefNumber>{refNumber}</RefNumber>");
            sb.Append($"<Memo>{memo}</Memo>");
            sb.Append("<ExpenseLineAdd>");
            sb.Append($"<AccountRef><FullName>{account}</FullName></AccountRef>");
            sb.Append($"<Amount>{amount}</Amount>");
            sb.Append($"<Memo>{memo}</Memo>");
            sb.Append("</ExpenseLineAdd>");
            sb.Append("</BillAdd>");
            sb.Append("</BillAddRq>");
            sb.Append("</QBXMLMsgsRq>");
            sb.Append("</QBXML>");
            return sb.ToString();
        }

        private string XmlEscape(string value) => SecurityElement.Escape(value) ?? string.Empty;
    }
}
