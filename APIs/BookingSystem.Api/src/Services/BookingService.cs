using System.Collections.Generic;
using System.Linq;
using BookingSystem.Api.Data;
using BookingSystem.Api.Data.Entities;
using BookingSystem.Api.Models;

namespace BookingSystem.Api.Services
{
    /// <summary>
    /// Joins users, events and bookings from the data store into the API response DTOs.
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly IDataStore _store;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="store">The data store.</param>
        public BookingService(IDataStore store)
        {
            _store = store;
        }

        /// <inheritdoc />
        public List<UserDto> GetUsers()
        {
            var eventsById = _store.GetEvents().ToDictionary(e => e.Id);
            var bookingsByUser = _store.GetBookings().ToLookup(b => b.UserId);

            return _store.GetUsers()
                .Select(u => ToUserDto(u, bookingsByUser[u.Id], eventsById))
                .ToList();
        }

        /// <inheritdoc />
        public List<EventDto> GetEvents()
        {
            return _store.GetEvents().Select(ToEventDto).ToList();
        }

        /// <inheritdoc />
        public List<BookingDto> GetBookings()
        {
            var usersById = _store.GetUsers().ToDictionary(u => u.Id);
            var eventsById = _store.GetEvents().ToDictionary(e => e.Id);

            return _store.GetBookings()
                .Where(b => usersById.ContainsKey(b.UserId) && eventsById.ContainsKey(b.EventId))
                .Select(b => new BookingDto
                {
                    User = new UserDto { Name = usersById[b.UserId].Name, Events = new List<EventDto>() },
                    Event = ToEventDto(eventsById[b.EventId])
                })
                .ToList();
        }

        private static UserDto ToUserDto(User user, IEnumerable<Booking> bookings, IReadOnlyDictionary<int, Event> eventsById)
        {
            return new UserDto
            {
                Name = user.Name,
                Events = bookings
                    .Where(b => eventsById.ContainsKey(b.EventId))
                    .Select(b => ToEventDto(eventsById[b.EventId]))
                    .ToList()
            };
        }

        private static EventDto ToEventDto(Event @event)
        {
            return new EventDto { Title = @event.Title, Date = @event.Date };
        }
        /// <summary>
        /// Gets the user with the specified ID, or null if not found.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>The user with the specified ID, or null if not found.</returns>
        public UserDto GetUserById(int id)
        {
            var user = _store.FindUser(id);
            if (user == null) return null;

            var eventsById = _store.GetEvents().ToDictionary(e => e.Id);
            return ToUserDto(user, _store.GetBookingsForUser(id), eventsById);
        }

        /// <summary>
        /// Gets the event with the specified ID, or null if not found.
        /// </summary>
        /// <param name="id">The ID of the event to retrieve.</param>
        /// <returns>The event with the specified ID, or null if not found.</returns>
        public EventDto GetEventById(int id)
        {
            var @event = _store.FindEvent(id);
            return @event == null ? null : ToEventDto(@event);
        }
    }
}
