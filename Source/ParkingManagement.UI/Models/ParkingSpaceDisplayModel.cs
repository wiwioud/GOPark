using ParkingManagement.Data.Models;

namespace ParkingManagement.UI.Models
{
    public class ParkingSpaceDisplayModel
    {
        public ParkingSpace Space { get; set; }
        
        public string StatusColor 
        {
            get
            {
                if (Space.Status == "Occupied") return "#EF4444"; // Red
                if (Space.Status == "Reserved") return "#F97316"; // Orange
                if (Space.Status == "Out of Order" || Space.Status == "Außer Betrieb") return "#94A3B8"; // Gray
                return "#22C55E"; // Green (Free)
            }
        }
        
        public string StatusBackground 
        {
            get
            {
                if (Space.Status == "Occupied") return "#FEF2F2";
                if (Space.Status == "Reserved") return "#FFF7ED";
                if (Space.Status == "Out of Order" || Space.Status == "Außer Betrieb") return "#F8FAFC";
                return "#F0FDF4";
            }
        }
        
        public string StatusText
        {
            get
            {
                if (Space.Status == "Occupied") return "BELEGT";
                if (Space.Status == "Reserved") return "RESERVIERT";
                if (Space.Status == "Out of Order" || Space.Status == "Außer Betrieb") return "GESPERRT";
                return "FREI";
            }
        }
        
        public string FutureReservationText { get; set; } = string.Empty;
        public bool HasFutureReservation => !string.IsNullOrEmpty(FutureReservationText);
        
        public string OccupantText { get; set; } = string.Empty;
        public bool HasOccupant => !string.IsNullOrEmpty(OccupantText);
    }
}
