using System.Windows;
using UI_Dat_Ve_May_Bay.Views.Components;

namespace UI_Dat_Ve_May_Bay.Services
{
    public static class DialogService
    {
        public static void ShowInfo(string message, string title = "Thông báo")
        {
            if (Application.Current?.Dispatcher == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomMessageBox(message, title, CustomMessageBox.MessageBoxType.Info);
                SetOwner(dialog);
                dialog.ShowDialog();
            });
        }

        public static void ShowSuccess(string message, string title = "Thành công")
        {
            if (Application.Current?.Dispatcher == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomMessageBox(message, title, CustomMessageBox.MessageBoxType.Success);
                SetOwner(dialog);
                dialog.ShowDialog();
            });
        }

        public static void ShowWarning(string message, string title = "Cảnh báo")
        {
            if (Application.Current?.Dispatcher == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomMessageBox(message, title, CustomMessageBox.MessageBoxType.Warning);
                SetOwner(dialog);
                dialog.ShowDialog();
            });
        }

        public static void ShowError(string message, string title = "Lỗi")
        {
            if (Application.Current?.Dispatcher == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomMessageBox(message, title, CustomMessageBox.MessageBoxType.Error);
                SetOwner(dialog);
                dialog.ShowDialog();
            });
        }

        public static bool ShowConfirm(string message, string title = "Xác nhận")
        {
            if (Application.Current?.Dispatcher == null) return false;
            bool result = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomMessageBox(message, title, CustomMessageBox.MessageBoxType.Confirmation);
                SetOwner(dialog);
                dialog.ShowDialog();
                result = dialog.Result;
            });
            return result;
        }

        private static void SetOwner(Window dialog)
        {
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                dialog.Owner = Application.Current.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
    }
}
