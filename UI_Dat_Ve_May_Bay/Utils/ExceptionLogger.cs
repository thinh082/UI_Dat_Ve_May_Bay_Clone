using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace UI_Dat_Ve_May_Bay.Utils
{
    public static class ExceptionLogger
    {
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "UI_Dat_Ve_errors.log");

        public static void InstallGlobalExceptionHandlers()
        {
            if (Application.Current != null)
                Application.Current.DispatcherUnhandledException += DispatcherUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private static void DispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log(e.Exception);
            MessageBox.Show($"Unhandled UI exception: {e.Exception.Message}\nLog: {LogPath}", "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown");
            Log(ex);
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log(e.Exception);
            e.SetObserved();
        }

        private static void Log(Exception ex)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { /* swallow logging failures */ }
        }
    }
}