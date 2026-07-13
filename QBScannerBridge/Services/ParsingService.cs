using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using QBScannerBridge.Models;

namespace QBScannerBridge.Services
{
    public class ParsingService
    {
        private static readonly HashSet<string> _vendorSkipWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "invoice", "bill", "receipt", "statement", "page", "date", "total", "subtotal",
            "to:", "from:", "ship to:", "bill to:", "sold to:", "remit to:"
        };

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
                .Where(x => x.Length >= 3)
                .Where(x => !_vendorSkipWords.Contains(x))
                .Where(x => !Regex.IsMatch(x, @"^\W+$"))
                .ToList();

            return lines.FirstOrDefault() ?? "Unknown Vendor";
        }

        private string ExtractInvoiceNumber(string text)
        {
            string[] patterns =
            {
                @"(?i)(?:invoice|inv)\s*(?:number|#|no\.?)?\s*[:\-]?\s*([A-Z0-9\-]+)",
                @"(?i)(?:po|purchase\s*order)\s*(?:number|#|no\.?)?\s*[:\-]?\s*([A-Z0-9\-]+)",
                @"(?i)(?:bill|receipt|order|ref|reference)\s*(?:number|#|no\.?)?\s*[:\-]?\s*([A-Z0-9\-]+)",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }

            return string.Empty;
        }

        private DateTime? ExtractDate(string text)
        {
            string[] patterns =
            {
                @"\b(\d{1,2}[/\-]\d{1,2}[/\-]\d{4})\b",
                @"\b(\d{4}-\d{2}-\d{2})\b",
                @"\b(\d{1,2}/\d{1,2}/\d{2})\b",
                @"(?i)\b((?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2},?\s+\d{4})\b",
                @"(?i)\b((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\.?\s+\d{1,2},?\s+\d{4})\b",
                @"(?i)\b(\d{1,2}\s+(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4})\b",
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
                @"(?i)total\s*(?:due|amount)?\s*[:\-]?\s*\$?\s*([0-9,]+\.[0-9]{2})",
                @"(?i)amount\s*due\s*[:\-]?\s*\$?\s*([0-9,]+\.[0-9]{2})",
                @"(?i)balance\s*due\s*[:\-]?\s*\$?\s*([0-9,]+\.[0-9]{2})"
            };

            foreach (var pattern in labeledPatterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    string value = match.Groups[1].Value.Replace(",", "");
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

            if (text.Contains("lkq") || text.Contains("parts") || text.Contains("dealer")
                || text.Contains("autozone") || text.Contains("auto zone")
                || text.Contains("napa") || text.Contains("o'reilly") || text.Contains("oreilly"))
                return "Parts Expense";

            if (text.Contains("tow") || text.Contains("glass") || text.Contains("sublet")
                || text.Contains("paint") || text.Contains("body shop") || text.Contains("labor"))
                return "Sublet Expense";

            if (text.Contains("office") || text.Contains("supply") || text.Contains("supplies")
                || text.Contains("staples") || text.Contains("amazon"))
                return "Office Supplies";

            if (text.Contains("fuel") || text.Contains("gasoline") || text.Contains("gas station"))
                return "Fuel Expense";

            return "Parts Expense";
        }
    }
}
