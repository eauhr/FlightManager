namespace FlightManager.Services.ExternalFlights;

public sealed class ExternalFlightSearchRequest
{
    public string DepartureAirport { get; set; } = ""; // IATA, e.g. SOF
    public string ArrivalAirport { get; set; } = "";   // IATA, e.g. LHR
    public DateOnly OutboundDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string? ClientTimeZoneId { get; set; }
    public bool RoundTrip => ReturnDate.HasValue;
    public int Adults { get; set; } = 1;
    public int Children { get; set; }
    public int InfantsInSeat { get; set; }
    public int InfantsOnLap { get; set; }
    public string TravelClass { get; set; } = "economy";
    public int Stops { get; set; } // 0 any, 1 nonstop, 2 <=1 stop, 3 <=2 stops
}
