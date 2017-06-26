using System.Data.Entity;
using ViennaParking.Data.Models;

namespace ViennaParking.Data
{
    public class ParkingDbContext : DbContext
    {
        public ParkingDbContext() : base("ParkingDB") { }
        public DbSet<ShortTermParkingZone> ShortTermParkingZones { get; set; }
        public DbSet<ViennaParking.Data.Models.TicketShop> TicketShop { get; set; }
    }
}
