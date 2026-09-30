namespace FlightManager.Services.ExternalFlights;

public interface IFlightSearchService
{
    Task<IReadOnlyList<ExternalFlightOffer>> SearchAsync(
        ExternalFlightSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingRequest?> ResolveBookingRequestAsync(
        string bookingToken,
        CancellationToken cancellationToken = default);
}
