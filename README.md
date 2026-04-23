# QBScannerBridge

Watches `C:\Scans\QB` for PDFs/images, OCRs them, parses basic invoice fields, and posts a **BillAdd** to **QuickBooks Desktop** using the **QuickBooks Desktop SDK** (QBXML Request Processor).

## What it does
- Folder watcher: `C:\Scans\QB`
- Supports: PDF (first page) + common image formats
- OCR: Tesseract (requires `tessdata\eng.traineddata`)
- Parse fields: vendor, invoice #, date, total
- Review/edit in a WPF UI
- Post to QuickBooks: `BillAddRq` via `QBXMLRP2Lib.RequestProcessor2`

## Requirements (QuickBooks computer)
1. **QuickBooks Desktop** installed (64-bit is fine)
2. **QuickBooks Desktop SDK** installed
3. Open **QuickBooks Desktop** and your **company file** before testing

## Build requirements
- Visual Studio 2022 (or 2019) with **.NET desktop development**
- .NET Framework **4.8**

## Setup steps
1. Open the solution in Visual Studio
2. Restore NuGet packages
3. Add COM reference:
   - Project → Add Reference… → COM
   - Select **QBXMLRP2 1.0 Type Library** (`QBXMLRP2Lib`)
4. Add Tesseract language data:
   - Create folder: `QBScannerBridge\tessdata`
   - Put file: `eng.traineddata` inside it
   - Set `eng.traineddata` to **Copy to Output Directory: Copy if newer**

## Run
1. Start the app
2. Click **Start Watching**
3. Drop a PDF/image into `C:\Scans\QB`
4. Review fields
5. Click **Post Bill to QuickBooks**

## Notes / troubleshooting
- If you see COM errors like **“Class not registered”**, try setting Platform Target to **x86**.
- QuickBooks will prompt to authorize the app the first time it connects.
