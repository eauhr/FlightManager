namespace FlightManager.Services.ExternalFlights;

public interface IFlightLocationSearchService
{
    Task<IReadOnlyList<FlightLocationSuggestion>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
