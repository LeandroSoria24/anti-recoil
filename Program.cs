using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace RecoilStudio
{
    static class EntryPoint
    {
        [STAThread]
        static void Main()
        {
            NativeInput.EnableDpiAwareness();

            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                LogError(e.Exception);
                MessageBox.Show(
                    "Ocurrió un error inesperado. Se guardó el detalle en error.log.\n\n" + e.Exception.Message,
                    "Recoil Studio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.Handled = true;
                app.Shutdown(1);
            };

            MainWindow window = new MainWindow();
            app.MainWindow = window;
            app.Run(window);
        }

        private static void LogError(Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                string entry = string.Format(
                    "[{0:yyyy-MM-dd HH:mm:ss}] {1}{2}{2}",
                    DateTime.Now,
                    ex != null ? ex.ToString() : "Unknown error",
                    Environment.NewLine);
                File.AppendAllText(logPath, entry, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
