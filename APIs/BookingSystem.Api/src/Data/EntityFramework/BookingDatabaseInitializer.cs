using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Api.Data.EntityFramework
{
    internal static class BookingDatabaseInitializer
    {
        public static void Initialize(BookingDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any() || context.Events.Any() || context.Bookings.Any())
            {
                return;
            }

            var seedData = BookingSystemSeedData.Load();
            context.Users.AddRange(seedData.Users);
            context.Events.AddRange(seedData.Events);
            context.Bookings.AddRange(seedData.Bookings);
            context.SaveChanges();
        }
    }
}
