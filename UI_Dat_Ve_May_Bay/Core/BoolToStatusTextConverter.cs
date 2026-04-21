using System;
using System.Globalization;
using System.Windows.Data;

namespace UI_Dat_Ve_May_Bay.Core
{
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? "ACTIVE" : "INACTIVE";
            return "UNKNOWN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
