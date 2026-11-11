using System;
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
            var usersById = _store.GetUsers().ToDictionary(u => u.Id);
            var bookingsByEvent = _store.GetBookings().ToLookup(b => b.EventId);

            return _store.GetEvents()
                .Select(e => ToEventDto(e, bookingsByEvent[e.Id], usersById))
                .ToList();
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
                    User = ToUserSummary(usersById[b.UserId]),
                    Event = ToEventSummary(eventsById[b.EventId])
                })
                .ToList();
        }

        private static UserDto ToUserDto(User user, IEnumerable<Booking> bookings, IReadOnlyDictionary<int, Event> eventsById)
        {
            var dto = ToUserSummary(user);
            dto.Events = bookings
                .Where(b => eventsById.ContainsKey(b.EventId))
                .Select(b => ToEventSummary(eventsById[b.EventId]))
                .ToList();
            return dto;
        }

        private static EventDto ToEventDto(Event @event, IEnumerable<Booking> bookings, IReadOnlyDictionary<int, User> usersById)
        {
            var dto = ToEventSummary(@event);
            dto.Users = bookings
                .Where(b => usersById.ContainsKey(b.UserId))
                .Select(b => ToUserSummary(usersById[b.UserId]))
                .ToList();
            return dto;
        }

        // Summaries leave the nested collection empty so a user's events do not list their users
        // again (and vice versa), which would otherwise be an endless cycle.
        private static UserDto ToUserSummary(User user)
        {
            return new UserDto { Id = user.Id, Name = user.Name, Events = new List<EventDto>() };
        }

        private static EventDto ToEventSummary(Event @event)
        {
            return new EventDto { Id = @event.Id, Title = @event.Title, Date = @event.Date, Users = new List<UserDto>() };
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
            if (@event == null) return null;

            var usersById = _store.GetUsers().ToDictionary(u => u.Id);
            var bookings = _store.GetBookings().Where(b => b.EventId == id);
            return ToEventDto(@event, bookings, usersById);
        }

        /// <summary>
        /// Creates a new booking.
        /// </summary>
        /// <param name="bookingPayload">The booking payload.</param>
        public int CreateBooking(BookingPayload bookingPayload)
        {
            var @user = _store.FindUser(bookingPayload.UserId) ?? throw new ArgumentException("User not found", nameof(bookingPayload));
            var @event = _store.FindEvent(bookingPayload.EventId) ?? throw new ArgumentException("Event not found", nameof(bookingPayload));
            
            return _store.CreateBooking(@user, @event);

        }
    }
}
