using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using QBScannerBridge.Models;

namespace QBScannerBridge.Services
{
    public class ParsingService
    {
        public ScanDocument Parse(string filePath, string rawText)
        {
            var doc = new ScanDocument
            {
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                RawText = rawText,
                VendorName = ExtractVendor(rawText),
                InvoiceNumber = ExtractInvoiceNumber(rawText),
                InvoiceDate = ExtractDate(rawText),
                TotalAmount = ExtractTotal(rawText),
                Memo = "Imported from scanned document"
            };

            doc.ExpenseAccount = GuessExpenseAccount(doc.VendorName, rawText);
            return doc;
        }

        private string ExtractVendor(string text)
        {
            var lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return lines.FirstOrDefault() ?? "Unknown Vendor";
        }

        private string ExtractInvoiceNumber(string text)
        {
            string[] patterns =
            {
                @"(?i)invoice\s*(number|#|no\.?)*\s*[:\-]?\s*([A-Z0-9\-]+)",
                @"(?i)inv\s*(number|#|no\.?)*\s*[:\-]?\s*([A-Z0-9\-]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                    return match.Groups[2].Value.Trim();
            }

            return string.Empty;
        }

        private DateTime? ExtractDate(string text)
        {
            string[] patterns =
            {
                @"\b(\d{1,2}/\d{1,2}/\d{2,4})\b",
                @"\b(\d{4}-\d{2}-\d{2})\b"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out DateTime dt))
                    return dt;
            }

            return null;
        }

        private decimal? ExtractTotal(string text)
        {
            string[] labeledPatterns =
            {
                @"(?i)total\s*(due|amount)?\s*[:\-]?\s*\$?\s*([0-9,]+\.[0-9]{2})",
                @"(?i)amount\s*due\s*[:\-]?\s*\$?\s*([0-9,]+\.[0-9]{2})",
                @"(?i)balance\s*due\s*[:\-]?\s*\$?\s*([0-9,]+\.[0-9]{2})"
            };

            foreach (var pattern in labeledPatterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    string value = match.Groups[match.Groups.Count - 1].Value.Replace(",", "");
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal total))
                        return total;
                }
            }

            var allMoney = Regex.Matches(text, @"\$?\s*([0-9,]+\.[0-9]{2})")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Replace(",", ""))
                .Select(v => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) ? (decimal?)d : null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            return allMoney.Count > 0 ? allMoney.Max() : null;
        }

        private string GuessExpenseAccount(string vendorName, string rawText)
        {
            string text = (vendorName + " " + rawText).ToLowerInvariant();

            if (text.Contains("lkq") || text.Contains("parts") || text.Contains("dealer"))
                return "Parts Expense";

            if (text.Contains("tow") || text.Contains("glass") || text.Contains("sublet"))
                return "Sublet Expense";

            return "Parts Expense";
        }
    }
}
