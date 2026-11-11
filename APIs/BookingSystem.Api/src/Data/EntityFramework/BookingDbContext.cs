using BookingSystem.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Api.Data.EntityFramework
{
    /// <summary>
    /// Entity Framework Core model for the booking API.
    /// </summary>
    public class BookingDbContext : DbContext
    {
        /// <summary>
        /// Creates the context with the configured database provider.
        /// </summary>
        public BookingDbContext(DbContextOptions<BookingDbContext> options)
            : base(options)
        {
        }

        /// <summary>Gets the users set.</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Gets the events set.</summary>
        public DbSet<Event> Events => Set<Event>();

        /// <summary>Gets the bookings set.</summary>
        public DbSet<Booking> Bookings => Set<Booking>();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(user => user.Id);
            modelBuilder.Entity<Event>().HasKey(@event => @event.Id);

            modelBuilder.Entity<Booking>(booking =>
            {
                booking.HasKey(item => item.Id);
                booking.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(item => item.UserId);
                booking.HasOne<Event>()
                    .WithMany()
                    .HasForeignKey(item => item.EventId);
            });
        }
    }
}
