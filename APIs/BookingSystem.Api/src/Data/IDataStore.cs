using System.Collections.Generic;
using BookingSystem.Api.Data.Entities;

namespace BookingSystem.Api.Data
{
    /// <summary>
    /// Data store holding the users, events and bookings.
    /// </summary>
    public interface IDataStore
    {
        /// <summary>Returns a snapshot of all users.</summary>
        IReadOnlyList<User> GetUsers();

        /// <summary>Returns a snapshot of all events.</summary>
        IReadOnlyList<Event> GetEvents();

        /// <summary>Returns a snapshot of all bookings.</summary>
        IReadOnlyList<Booking> GetBookings();

        /// <summary>Finds a user by id, or null when not found.</summary>
        User FindUser(int id);

        /// <summary>Finds a user by name (case-insensitive), or null when not found.</summary>
        User FindUserByName(string name);

        /// <summary>Finds an event by id, or null when not found.</summary>
        Event FindEvent(int id);

        /// <summary>Finds an event by title (case-insensitive), or null when not found.</summary>
        Event FindEventByTitle(string title);

        /// <summary>Finds a booking by id, or null when not found.</summary>
        Booking FindBooking(int id);

        /// <summary>Returns the bookings of a user.</summary>
        IReadOnlyList<Booking> GetBookingsForUser(int userId);

        /// <summary>Adds a user and assigns its id.</summary>
        User AddUser(User user);

        /// <summary>Adds an event and assigns its id.</summary>
        Event AddEvent(Event @event);

        /// <summary>Adds a booking and assigns its id.</summary>
        Booking AddBooking(Booking booking);


        /// <summary>
        /// Creates a booking for the specified user and event.
        /// </summary>
        /// <param name="theUser">The user for whom to create the booking.</param>
        /// <param name="theEvent">The event for which to create the booking.</param>
        /// <returns>The ID of the created booking.</returns>
        int CreateBooking(User theUser, Event theEvent);
    }
}
