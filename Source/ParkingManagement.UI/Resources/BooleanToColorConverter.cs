using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ParkingManagement.UI.Resources
{
    public class BooleanToColorConverter : IValueConverter
    {
        public SolidColorBrush TrueColor { get; set; } = new SolidColorBrush(Colors.Green);
        public SolidColorBrush FalseColor { get; set; } = new SolidColorBrush(Colors.Red);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? TrueColor : FalseColor;
            }
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
