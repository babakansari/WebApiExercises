using System.Collections.Generic;
using BookingSystem.Api.Models;

namespace BookingSystem.Api.Services
{
    /// <summary>
    /// Builds the API response DTOs from the data store.
    /// </summary>
    public interface IBookingService
    {
        /// <summary>Creates a new booking.</summary>
        int CreateBooking(BookingPayload bookingPayload);

        /// <summary>Returns every user with the events the user is booked into.</summary>
        List<UserDto> GetUsers();

        /// <summary>Returns every event.</summary>
        List<EventDto> GetEvents();

        /// <summary>Returns every booking with its user and event.</summary>
        List<BookingDto> GetBookings();

        /// <summary>Returns the user with the given id, or null when not found.</summary>
        UserDto GetUserById(int id);

        /// <summary>Returns the event with the given id, or null when not found.</summary>
        EventDto GetEventById(int id);
    }
}
