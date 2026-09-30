namespace FlightManager.Services.ExternalFlights;

public sealed record FlightLocationSuggestion
{
    public string Code { get; init; } = "";
    public string AirportName { get; init; } = "";
    public string City { get; init; } = "";
    public string Country { get; init; } = "";

    public string DisplayName =>
        string.IsNullOrWhiteSpace(City)
            ? $"{AirportName} ({Code})"
            : $"{AirportName} ({Code})";

    public string SecondaryText =>
        string.Join(", ", new[] { City, Country }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
