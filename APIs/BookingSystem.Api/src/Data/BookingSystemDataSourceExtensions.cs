using System;
using BookingSystem.Api.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Api.Data
{
    /// <summary>
    /// Registers the booking API data source.
    /// </summary>
    public static class BookingSystemDataSourceExtensions
    {
        /// <summary>
        /// Registers the configured booking data store and its dependencies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddBookingSystemDataSource(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var dataStoreProvider = configuration["DataStore:Provider"] ?? "InMemory";
            if (string.Equals(dataStoreProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IDataStore, InMemoryDataStore>();
                return services;
            }

            if (string.Equals(dataStoreProvider, "EfCoreInMemory", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<BookingDbContext>(options =>
                    options.UseInMemoryDatabase("BookingSystem"));
                services.AddScoped<IDataStore, EfCoreDataStore>();
                return services;
            }

            throw new InvalidOperationException(
                $"Unsupported DataStore:Provider '{dataStoreProvider}'. Supported values are 'InMemory' and 'EfCoreInMemory'.");
        }
    }
}
