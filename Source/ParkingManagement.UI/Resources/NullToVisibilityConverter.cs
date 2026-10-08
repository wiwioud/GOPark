using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ParkingManagement.UI.Resources
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object VisibilityWhenNull { get; set; } = Visibility.Collapsed;
        public object VisibilityWhenNotNull { get; set; } = Visibility.Visible;

        public object VisibilityWhenEmptyString { get; set; } = Visibility.Collapsed;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return VisibilityWhenNull;
            }

            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                return VisibilityWhenEmptyString;
            }

            return VisibilityWhenNotNull;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
