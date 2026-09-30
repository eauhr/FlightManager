namespace FlightManager.Services.ExternalFlights;

public sealed class BookingRequest
{
    public string Url { get; init; } = "";

    public string? PostData { get; init; }
}
