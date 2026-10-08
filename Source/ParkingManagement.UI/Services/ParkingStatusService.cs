using Microsoft.EntityFrameworkCore;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingManagement.UI.Services
{
    public class ParkingStatusService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ParkingStatusService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task SyncAllSpacesAsync()
        {
            using var db = _dbContextFactory.CreateDbContext();
            var spaces = await db.ParkingSpaces.ToListAsync();
            
            var now = DateTime.Now;
            
            var activeSessions = await db.ParkingSessions
                .Where(s => s.Status == "Active")
                .ToListAsync();
                
            var activeReservations = await db.Reservations
                .Where(r => r.Status != "Cancelled" && r.Status != "Completed" 
                            && r.ReservationDate.Date == now.Date 
                            && r.StartTime <= now.TimeOfDay 
                            && r.EndTime >= now.TimeOfDay)
                .ToListAsync();

            bool changed = false;

            foreach (var space in spaces)
            {
                if (space.Status == "Out of Order" || space.Status == "Außer Betrieb") continue;

                var isOccupied = activeSessions.Any(s => s.ParkingSpaceId == space.Id);
                var isReserved = activeReservations.Any(r => r.ParkingSpaceId == space.Id);
                
                string newStatus = "Free";
                if (isOccupied) newStatus = "Occupied";
                else if (isReserved) newStatus = "Reserved";

                if (space.Status != newStatus)
                {
                    space.Status = newStatus;
                    changed = true;
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
