using System;
using System.Windows;
using System.Windows.Threading;

namespace UI_Dat_Ve_May_Bay.Core
{
    public class AdminToastMessage
    {//
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
    }

    public static class AdminUiService
    {
        public static event Action<AdminToastMessage>? ToastRequested;

        public static void Publish(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var trimmed = message.Trim();
            var normalized = trimmed.ToLowerInvariant();
            if (normalized.StartsWith("da tai")
                || normalized.StartsWith("đã tải")
                || normalized.StartsWith("khong co")
                || normalized.StartsWith("không có")
                || normalized.StartsWith("khong tim thay")
                || normalized.StartsWith("không tìm thấy")
                || normalized.StartsWith("chua co")
                || normalized.StartsWith("chưa có"))
                return;

            var type = normalized.Contains("lỗi") || normalized.Contains("loi") || normalized.Contains("thất bại") || normalized.Contains("that bai")
                ? "error"
                : normalized.Contains("không") || normalized.Contains("khong") || normalized.Contains("chưa") || normalized.Contains("chua")
                    ? "warning"
                    : "success";

            Raise(new AdminToastMessage { Message = trimmed, Type = type });
        }

        public static bool ConfirmDelete(string message)
        {
            return UI_Dat_Ve_May_Bay.Services.DialogService.ShowConfirm(message, "Xác nhận thao tác");
        }

        private static void Raise(AdminToastMessage toast)
        {
            if (Application.Current?.Dispatcher == null)
            {
                ToastRequested?.Invoke(toast);
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                ToastRequested?.Invoke(toast);
                return;
            }

            Application.Current.Dispatcher.Invoke(() => ToastRequested?.Invoke(toast));
        }
    }
}
