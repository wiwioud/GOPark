namespace ParkingManagement.Data.Models
{
    public class ParkingRateSetting
    {
        public int Id { get; set; }
        
        // Fee for one hour
        public decimal HourlyRate { get; set; }
        
        // Minimum fee even if parked for 5 mins
        public decimal MinimumCharge { get; set; }
        
        // Time before being charged (e.g., 15 mins drop off)
        public int GracePeriodMinutes { get; set; }
        
        // Maximum fee per 24 hours
        public decimal MaximumDailyRate { get; set; }
    }
}
