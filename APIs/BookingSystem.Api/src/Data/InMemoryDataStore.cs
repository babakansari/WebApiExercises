using System;
using System.Collections.Generic;
using System.Linq;
using BookingSystem.Api.Models;

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
        private int _nextUserId = 1;
        private int _nextEventId = 1;
        private int _nextBookingId = 1;

        /// <summary>
        /// Creates the store and seeds it with sample data.
        /// </summary>
        public InMemoryDataStore()
        {
            var alice = AddUser(new User { Name = "Alice" });
            var bob = AddUser(new User { Name = "Bob" });
            AddUser(new User { Name = "Carol" });

            var today = DateTime.UtcNow.Date;
            var concert = AddEvent(new Event { Title = "Concert", Date = today.AddDays(7) });
            var conference = AddEvent(new Event { Title = "Conference", Date = today.AddDays(14) });
            AddEvent(new Event { Title = "Workshop", Date = today.AddDays(21) });

            AddBooking(new Booking { UserId = alice.Id, EventId = concert.Id });
            AddBooking(new Booking { UserId = alice.Id, EventId = conference.Id });
            AddBooking(new Booking { UserId = bob.Id, EventId = concert.Id });
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
    }
}
