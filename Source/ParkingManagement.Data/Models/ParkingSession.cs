using System;

namespace ParkingManagement.Data.Models
{
    public class ParkingSession
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        public int ParkingSpaceId { get; set; }
        public ParkingSpace ParkingSpace { get; set; } = null!;
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public decimal? ParkingFee { get; set; }
        public string Status { get; set; } = "Active"; // Active, Completed
    }
}
