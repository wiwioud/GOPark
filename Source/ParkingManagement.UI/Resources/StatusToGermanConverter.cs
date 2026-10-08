using System;
using System.Globalization;
using System.Windows.Data;

namespace ParkingManagement.UI.Resources
{
    public class StatusToGermanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value?.ToString() ?? string.Empty) switch
            {
                // Space statuses
                "Free"         => "Frei",
                "Occupied"     => "Belegt",
                "Reserved"     => "Reserviert",
                "Out of Order" => "Außer Betrieb",
                // Session statuses
                "Active"       => "Aktiv",
                "Completed"    => "Abgeschlossen",
                // Reservation statuses
                "Planned"      => "Geplant",
                "Expired"      => "Abgelaufen",
                "Cancelled"    => "Storniert",
                // Role
                "Administrator" => "Administrator",
                "Employee"     => "Mitarbeiter",
                // Fallback
                var s          => s
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
