using System;
using System.Collections.Generic;
using System.IO;
using BookingSystem.Api.Data.Entities;
using Newtonsoft.Json.Linq;

namespace BookingSystem.Api.Data
{
    internal static class BookingSystemSeedData
    {
        public static (List<User> Users, List<Event> Events, List<Booking> Bookings) Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "BookingSystem.json");
            var data = JObject.Parse(File.ReadAllText(path));

            return (
                data["users"]?.ToObject<List<User>>() ?? new List<User>(),
                data["events"]?.ToObject<List<Event>>() ?? new List<Event>(),
                data["bookings"]?.ToObject<List<Booking>>() ?? new List<Booking>());
        }
    }
}
