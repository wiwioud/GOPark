using System;

namespace ParkingManagement.Data.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        public int ParkingSpaceId { get; set; }
        public ParkingSpace ParkingSpace { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
                public string Status { get; set; } = "Active";
        public string Notes { get; set; } = string.Empty; // Active, Completed, Cancelled
    }
}

