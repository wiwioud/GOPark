namespace ParkingManagement.Data.Models
{
    public class ParkingSpace
    {
        public int Id { get; set; }
        public string SpaceNumber { get; set; } = string.Empty;
                public string Status { get; set; } = "Free";
        public string Zone { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Free, Occupied, Reserved
    }
}

