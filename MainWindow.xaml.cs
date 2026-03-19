using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using DirectXTexNet;

namespace BulkDDSConverter;

public partial class MainWindow : Window {
    private CancellationTokenSource? _cts;

    public MainWindow() {
        try {
            InitializeComponent();

            var helper = TexHelper.Instance;
            this.Title = "Bulk DDS Converter (Engine Ready)";
        } catch (Exception ex) {
            MessageBox.Show($"Critical Dependency Error: {ex.Message}\n\n" + "Ensure you have the Visual C++ Redistributable installed.", "Startup Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e) {
        var dialog = new OpenFolderDialog {
            Title = "Select the root folder containing textures",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true) {
            FolderBox.Text = dialog.FolderName;
            FolderBox.Foreground = System.Windows.Media.Brushes.WhiteSmoke;
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) {
        _cts?.Cancel();
        CancelBtn.IsEnabled = false;
        StatusLabel.Text = "SIGNALING ABORT...";
        AppendLog("\n[WARN] Abort signal sent. Stopping after current file...");
    }

    private async void ConvertBtn_Click(object sender, RoutedEventArgs e) {
        if (string.IsNullOrWhiteSpace(FolderBox.Text) || FolderBox.Text.StartsWith("Select")) {
            MessageBox.Show("Please select a root folder first.", "No Folder Selected");
            return;
        }

        var format = (DdsFormat)FormatSelector.SelectedIndex;
        var rootPath = FolderBox.Text;

        ConvertBtn.IsEnabled = false;
        CancelBtn.Visibility = Visibility.Visible;
        CancelBtn.IsEnabled = true;
        LogBox.Clear();
        ProgressBar.Value = 0;

        _cts = new CancellationTokenSource();

        try {
            await Task.Run(() => RunConversion(rootPath, format, _cts.Token), _cts.Token);
            StatusLabel.Text = _cts.Token.IsCancellationRequested ? "OPERATION ABORTED" : "CONVERSION COMPLETE";
        } catch (Exception ex) {
            AppendLog($"[ERROR] {ex.Message}");
            StatusLabel.Text = "SYSTEM ERROR";
        } finally {
            ConvertBtn.IsEnabled = true;
            CancelBtn.Visibility = Visibility.Collapsed;
            _cts.Dispose();
            _cts = null;
        }
    }

    private void RunConversion(string rootPath, DdsFormat format, CancellationToken token) {
        var files = TextureProcessor.DiscoverFiles(rootPath).ToList();
        if (files.Count == 0) {
            Dispatcher.Invoke(() => {
                StatusLabel.Text = "No supported files found.";
                AppendLog("[INFO] No supported images found in the selected folder.");
            });
            return;
        }

        Dispatcher.Invoke(() => {
            ProgressBar.Maximum = files.Count;
            AppendLog($"[INFO] Found {files.Count} files. Starting process...\n");
        });

        var start = DateTime.Now;

        int done = 0;
        foreach (var file in files) {
            if (token.IsCancellationRequested) break;

            var fileName = Path.GetFileName(file);
            Dispatcher.Invoke(() => StatusLabel.Text = $"PROCESSING: {fileName.ToUpper()}");

            bool useGpu = false;

            Dispatcher.Invoke(() =>
            {
                useGpu = GpuToggle.IsChecked == true;
            });

            var result = TextureProcessor.Convert(file, format, useGpu);

            done++;

            var elapsed = (DateTime.Now - start).TotalSeconds;
            var speed = done / Math.Max(elapsed, 0.1);

            Dispatcher.Invoke(() =>
            {
                StatusLabel.Text = $"PROCESSING: {fileName.ToUpper()}";
                FileCountLabel.Text = $"FILES: {files.Count}";
                SpeedLabel.Text = $"{speed:F1} files/sec";
                ProgressBar.Value = done;

                if (result.Success)
                    AppendLog($"[OK]   {fileName}");
                else
                    AppendLog($"[FAIL] {fileName} | {result.Error}");
            });
        }
    }

    private void AppendLog(string message) {
        Dispatcher.Invoke(() => {
            LogBox.AppendText(message + "\n");
            LogBox.ScrollToEnd();
        });
    }

    private void Window_DragOver(object sender, DragEventArgs e) {
        DropOverlay.Visibility = Visibility.Visible;
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e) {
        DropOverlay.Visibility = Visibility.Collapsed;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var path = files[0];

        FolderBox.Text = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

        UpdateFileCount();
    }

    private void UpdateFileCount() {
        var files = TextureProcessor.DiscoverFiles(FolderBox.Text).ToList();
        FileCountLabel.Text = $"FILES: {files.Count}";
    }

}