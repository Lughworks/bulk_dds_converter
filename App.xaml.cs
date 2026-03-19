using System.Windows;

namespace BulkDDSConverter;

public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        this.DispatcherUnhandledException += (s, args) => {
            MessageBox.Show($"UI Thread Error: {args.Exception.Message}", "Fatal Error");
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) => {
            MessageBox.Show($"System Error: {args.ExceptionObject}", "Critical Halt");
        };

        base.OnStartup(e);
    }
}