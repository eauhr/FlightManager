namespace FlightManager.Services.ExternalFlights;

public sealed class ExternalFlightOffer
{
    public string Provider { get; init; } = "Google Flights / SerpApi";
    public string Airline { get; init; } = "";
    public string FlightNumbers { get; init; } = "";
    public string Origin { get; init; } = "";
    public string Destination { get; init; } = "";
    public DateTime Departure { get; init; }
    public DateTime Arrival { get; init; }
    public int Stops { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? BookingToken { get; init; }
    public string? BookingUrl { get; init; }
    public string? BookingPostData { get; init; }
    public string? LogoUrl { get; init; }
}
