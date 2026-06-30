using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using QBScannerBridge.Models;

namespace QBScannerBridge
{
    public partial class ProductionBoardWindow : Window
    {
        private readonly ObservableCollection<ScanDocument> _source;
        private readonly DispatcherTimer _refreshTimer;

        public ObservableCollection<ScanDocument> Incoming   { get; } = new ObservableCollection<ScanDocument>();
        public ObservableCollection<ScanDocument> Processing { get; } = new ObservableCollection<ScanDocument>();
        public ObservableCollection<ScanDocument> Review     { get; } = new ObservableCollection<ScanDocument>();
        public ObservableCollection<ScanDocument> Posting    { get; } = new ObservableCollection<ScanDocument>();
        public ObservableCollection<ScanDocument> Done       { get; } = new ObservableCollection<ScanDocument>();
        public ObservableCollection<ScanDocument> Failed     { get; } = new ObservableCollection<ScanDocument>();

        public ProductionBoardWindow(ObservableCollection<ScanDocument> docs)
        {
            InitializeComponent();
            DataContext = this;

            _source = docs;
            _source.CollectionChanged += (s, e) => Refresh();

            // Periodic refresh picks up in-place status changes (ScanDocument has no INPC)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += (s, e) => Refresh();
            _refreshTimer.Start();

            Refresh();
        }

        private void Refresh()
        {
            Sync(Incoming,   _source.Where(d => IsIncoming(d.Status)));
            Sync(Processing, _source.Where(d => IsProcessing(d.Status)));
            Sync(Review,     _source.Where(d => IsReview(d.Status)));
            Sync(Posting,    _source.Where(d => IsPosting(d.Status)));
            Sync(Done,       _source.Where(d => IsDone(d.Status)));
            Sync(Failed,     _source.Where(d => IsFailed(d.Status)));
        }

        // Synchronise a column collection with a filtered snapshot without rebuilding it entirely,
        // so that WPF bindings only fire for actual add/remove changes.
        private static void Sync(ObservableCollection<ScanDocument> col, IEnumerable<ScanDocument> items)
        {
            var list = items.ToList();

            for (int i = col.Count - 1; i >= 0; i--)
            {
                if (!list.Contains(col[i]))
                    col.RemoveAt(i);
            }

            foreach (var item in list)
            {
                if (!col.Contains(item))
                    col.Add(item);
            }
        }

        private static bool IsIncoming(string s)   => s == "Detected" || (s != null && s.Contains("Waiting"));
        private static bool IsProcessing(string s) => s != null && s.Contains("OCR");
        private static bool IsReview(string s)     => s == "Ready to review";
        private static bool IsPosting(string s)    => s != null && s.Contains("Posting");
        private static bool IsDone(string s)       => s == "Posted";
        private static bool IsFailed(string s)     => s != null &&
            (s.Contains("Error") || s.Contains("failed") || s.Contains("locked") || s.Contains("not ready"));

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer.Stop();
            base.OnClosed(e);
        }
    }
}
