using System;
using System.Collections.Generic;
using System.Linq;
using BookingSystem.Api.Data.Entities;

namespace BookingSystem.Api.Data.EntityFramework
{
    /// <summary>
    /// EF Core implementation of the booking API data store.
    /// </summary>
    public class EfCoreDataStore : IDataStore
    {
        private static readonly object IdLock = new object();
        private readonly BookingDbContext _context;

        /// <summary>
        /// Creates the store.
        /// </summary>
        /// <param name="context">The database context.</param>
        public EfCoreDataStore(BookingDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IReadOnlyList<User> GetUsers() => _context.Users.ToList();

        /// <inheritdoc />
        public IReadOnlyList<Event> GetEvents() => _context.Events.ToList();

        /// <inheritdoc />
        public IReadOnlyList<Booking> GetBookings() => _context.Bookings.ToList();

        /// <inheritdoc />
        public User FindUser(int id) => _context.Users.FirstOrDefault(user => user.Id == id);

        /// <inheritdoc />
        public User FindUserByName(string name) =>
            _context.Users.ToList().FirstOrDefault(user =>
                string.Equals(user.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc />
        public Event FindEvent(int id) => _context.Events.FirstOrDefault(@event => @event.Id == id);

        /// <inheritdoc />
        public Event FindEventByTitle(string title) =>
            _context.Events.ToList().FirstOrDefault(@event =>
                string.Equals(@event.Title, title, StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc />
        public Booking FindBooking(int id) => _context.Bookings.FirstOrDefault(booking => booking.Id == id);

        /// <inheritdoc />
        public IReadOnlyList<Booking> GetBookingsForUser(int userId) =>
            _context.Bookings.Where(booking => booking.UserId == userId).ToList();

        /// <inheritdoc />
        public User AddUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            lock (IdLock)
            {
                user.Id = _context.Users.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
                _context.Users.Add(user);
                _context.SaveChanges();
                return user;
            }
        }

        /// <inheritdoc />
        public Event AddEvent(Event @event)
        {
            ArgumentNullException.ThrowIfNull(@event);
            lock (IdLock)
            {
                @event.Id = _context.Events.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
                _context.Events.Add(@event);
                _context.SaveChanges();
                return @event;
            }
        }

        /// <inheritdoc />
        public Booking AddBooking(Booking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);
            lock (IdLock)
            {
                booking.Id = _context.Bookings.Select(item => item.Id).DefaultIfEmpty().Max() + 1;
                _context.Bookings.Add(booking);
                _context.SaveChanges();
                return booking;
            }
        }

        /// <inheritdoc />
        public int CreateBooking(User theUser, Event theEvent)
        {
            ArgumentNullException.ThrowIfNull(theUser);
            ArgumentNullException.ThrowIfNull(theEvent);

            lock (IdLock)
            {
                var booking = new Booking
                {
                    Id = _context.Bookings.Select(item => item.Id).DefaultIfEmpty().Max() + 1,
                    UserId = theUser.Id,
                    EventId = theEvent.Id
                };
                _context.Bookings.Add(booking);
                _context.SaveChanges();
                return booking.Id;
            }
        }
    }
}
