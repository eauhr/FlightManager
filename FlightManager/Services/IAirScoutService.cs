using FlightManager.Models.AirScout;
using FlightManager.Services.ExternalFlights;

namespace FlightManager.Services;

public interface IAirScoutService
{
    Task<AirScoutHomeViewModel> BuildHomeViewAsync(CancellationToken cancellationToken = default);

    Task<AirScoutSearchResultsViewModel> SearchAsync(
        AirScoutSearchRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<AirScoutBestOffersViewModel> GetBestOffersAsync(CancellationToken cancellationToken = default);

    Task<BookingRequest?> ResolveBookingRequestAsync(
        string bookingToken,
        CancellationToken cancellationToken = default);
}
