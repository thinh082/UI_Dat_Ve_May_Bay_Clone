using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UI_Dat_Ve_May_Bay.Converters
{
    public class AdminStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            var mode = parameter?.ToString()?.ToLowerInvariant() ?? "background";

            var (background, foreground) = Resolve(text);
            return mode == "foreground" ? foreground : background;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        private static (Brush Background, Brush Foreground) Resolve(string text)
        {
            if (text.Contains("hủy") || text.Contains("huy") || text.Contains("thất bại") || text.Contains("that bai"))
                return (new SolidColorBrush(Color.FromRgb(254, 226, 226)), new SolidColorBrush(Color.FromRgb(185, 28, 28)));

            if (text.Contains("checkin") || text.Contains("thành công") || text.Contains("thanh cong") || text.Contains("active") || text.Contains("kích hoạt") || text.Contains("kich hoat"))
                return (new SolidColorBrush(Color.FromRgb(219, 234, 254)), new SolidColorBrush(Color.FromRgb(29, 78, 216)));

            if (text.Contains("đã đặt") || text.Contains("da dat") || text.Contains("admin") || text.Contains("user") || text.Contains("thành viên") || text.Contains("thanh vien"))
                return (new SolidColorBrush(Color.FromRgb(220, 252, 231)), new SolidColorBrush(Color.FromRgb(21, 128, 61)));

            return (new SolidColorBrush(Color.FromRgb(241, 245, 249)), new SolidColorBrush(Color.FromRgb(51, 65, 85)));
        }
    }
}
