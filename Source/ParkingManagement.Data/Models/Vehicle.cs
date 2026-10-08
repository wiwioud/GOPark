namespace ParkingManagement.Data.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
                public string OwnerName { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}

