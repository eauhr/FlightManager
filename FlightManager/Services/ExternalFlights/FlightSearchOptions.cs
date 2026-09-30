namespace FlightManager.Services.ExternalFlights;

public sealed class FlightSearchOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://serpapi.com/search.json";
    public string Country { get; set; } = "bg";
    public string Language { get; set; } = "en";
    public string Currency { get; set; } = "EUR";
    public int SearchCacheSeconds { get; set; } = 90;
    public bool DeepSearch { get; set; }
}
