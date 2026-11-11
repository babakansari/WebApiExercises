using System;
using System.Collections.Generic;
using System.Linq;
using BookingSystem.Api.Data.Entities;

namespace BookingSystem.Api.Data
{
    /// <summary>
    /// Thread-safe in-memory <see cref="IDataStore"/> seeded with sample data.
    /// Register as a singleton; data is lost when the application stops.
    /// </summary>
    public class InMemoryDataStore : IDataStore
    {
        private readonly object _lock = new object();
        private readonly List<User> _users = new List<User>();
        private readonly List<Event> _events = new List<Event>();
        private readonly List<Booking> _bookings = new List<Booking>();
        private int _nextUserId;
        private int _nextEventId;
        private int _nextBookingId;

        /// <summary>
        /// Creates the store and loads its initial data from <c>Data/BookingSystem.json</c>.
        /// The id counters continue after the highest loaded id.
        /// </summary>
        public InMemoryDataStore()
        {
            var seedData = BookingSystemSeedData.Load();
            _users.AddRange(seedData.Users);
            _events.AddRange(seedData.Events);
            _bookings.AddRange(seedData.Bookings);

            _nextUserId = _users.Select(u => u.Id).DefaultIfEmpty(0).Max() + 1;
            _nextEventId = _events.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;
            _nextBookingId = _bookings.Select(b => b.Id).DefaultIfEmpty(0).Max() + 1;
        }

        /// <inheritdoc />
        public IReadOnlyList<User> GetUsers()
        {
            lock (_lock) { return _users.ToList(); }
        }

        /// <inheritdoc />
        public IReadOnlyList<Event> GetEvents()
        {
            lock (_lock) { return _events.ToList(); }
        }

        /// <inheritdoc />
        public IReadOnlyList<Booking> GetBookings()
        {
            lock (_lock) { return _bookings.ToList(); }
        }

        /// <inheritdoc />
        public User FindUser(int id)
        {
            lock (_lock) { return _users.FirstOrDefault(u => u.Id == id); }
        }

        /// <inheritdoc />
        public User FindUserByName(string name)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <inheritdoc />
        public Event FindEvent(int id)
        {
            lock (_lock) { return _events.FirstOrDefault(e => e.Id == id); }
        }

        /// <inheritdoc />
        public Event FindEventByTitle(string title)
        {
            lock (_lock)
            {
                return _events.FirstOrDefault(e => string.Equals(e.Title, title, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <inheritdoc />
        public Booking FindBooking(int id)
        {
            lock (_lock) { return _bookings.FirstOrDefault(b => b.Id == id); }
        }

        /// <inheritdoc />
        public IReadOnlyList<Booking> GetBookingsForUser(int userId)
        {
            lock (_lock) { return _bookings.Where(b => b.UserId == userId).ToList(); }
        }

        /// <inheritdoc />
        public User AddUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            lock (_lock)
            {
                user.Id = _nextUserId++;
                _users.Add(user);
                return user;
            }
        }

        /// <inheritdoc />
        public Event AddEvent(Event @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            lock (_lock)
            {
                @event.Id = _nextEventId++;
                _events.Add(@event);
                return @event;
            }
        }

        /// <inheritdoc />
        public Booking AddBooking(Booking booking)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));
            lock (_lock)
            {
                booking.Id = _nextBookingId++;
                _bookings.Add(booking);
                return booking;
            }
        }
        /// <inheritdoc />
        public int CreateBooking(User theUser, Event theEvent)
        {
            ArgumentNullException.ThrowIfNull(theUser);
            ArgumentNullException.ThrowIfNull(theEvent);

            lock (_lock)
            {
                var booking = new Booking
                {
                    Id = _nextBookingId++,
                    UserId = theUser.Id,
                    EventId = theEvent.Id
                };
                _bookings.Add(booking);
                return booking.Id;
            }
        }
    }
}
