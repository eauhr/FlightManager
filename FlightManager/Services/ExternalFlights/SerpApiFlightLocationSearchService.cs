using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace FlightManager.Services.ExternalFlights;

public sealed class SerpApiFlightLocationSearchService : IFlightLocationSearchService
{
    private readonly HttpClient _http;
    private readonly FlightSearchOptions _options;
    private readonly IMemoryCache _cache;

    public SerpApiFlightLocationSearchService(
        HttpClient http,
        IOptions<FlightSearchOptions> options,
        IMemoryCache cache)
    {
        _http = http;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<IReadOnlyList<FlightLocationSuggestion>> SearchAsync(
      string query,
      CancellationToken cancellationToken = default)
    {
        string cleaned = query.Trim();

        if (cleaned.Length < 2)
            return Array.Empty<FlightLocationSuggestion>();

        string cacheKey =
            $"flight-location:{_options.Country}:{_options.Language}:{cleaned.ToLowerInvariant()}";

        if (_cache.TryGetValue(
            cacheKey,
            out IReadOnlyList<FlightLocationSuggestion>? cached) &&
            cached != null)
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "SerpApi API key is missing. Add SerpApi:ApiKey to configuration or user secrets.");
        }

        var queryParams = new Dictionary<string, string>
        {
            ["engine"] = "google_flights_autocomplete",
            ["api_key"] = _options.ApiKey,
            ["q"] = cleaned,
            ["exclude_regions"] = "true",
            ["gl"] = _options.Country,
            ["hl"] = _options.Language
        };

        string url =
            _options.BaseUrl +
            "?" +
            string.Join(
                "&",
                queryParams.Select(p =>
                    $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        HttpResponseMessage response;

        try
        {
            response = await _http.GetAsync(
                url,
                cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpRequestException(
                "SerpApi airport autocomplete timed out.");
        }

        string body =
            await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Flight location search failed " +
                $"({(int)response.StatusCode}): {body}");
        }

        using JsonDocument json =
            JsonDocument.Parse(body);

        if (json.RootElement.TryGetProperty(
            "error",
            out JsonElement error))
        {
            string errorMessage =
                error.GetString() ??
                "SerpApi returned an error.";

            bool isNoResultsError =
                errorMessage.Contains("hasn't returned any results", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("no results", StringComparison.OrdinalIgnoreCase);

            if (isNoResultsError)
            {
                return Array.Empty<FlightLocationSuggestion>();
            }

            throw new InvalidOperationException(errorMessage);
        }

        List<FlightLocationSuggestion> results = new();

        if (!json.RootElement.TryGetProperty(
            "suggestions",
            out JsonElement suggestions) ||
            suggestions.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<FlightLocationSuggestion>();
        }

        foreach (JsonElement suggestion in suggestions.EnumerateArray())
        {

            if (!suggestion.TryGetProperty(
                    "airports",
                    out JsonElement airports) ||
                airports.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            string city =
                suggestion.TryGetProperty(
                    "name",
                    out JsonElement nameElement)
                    ? nameElement.GetString() ?? ""
                    : "";

            string description =
                suggestion.TryGetProperty(
                    "description",
                    out JsonElement descriptionElement)
                    ? descriptionElement.GetString() ?? ""
                    : "";

            string country =
                ExtractCountry(description);

            foreach (JsonElement airport in airports.EnumerateArray())
            {
                string code =
                    airport.TryGetProperty(
                        "id",
                        out JsonElement idElement)
                        ? idElement.GetString() ?? ""
                        : "";


                if (code.Length != 3 ||
                    code.Any(c => !char.IsLetter(c)))
                {
                    continue;
                }

                code = code.ToUpperInvariant();

                string airportName =
                    airport.TryGetProperty(
                        "name",
                        out JsonElement airportNameElement)
                        ? airportNameElement.GetString() ?? "Airport"
                        : "Airport";

                string airportCity =
                    airport.TryGetProperty(
                        "city",
                        out JsonElement airportCityElement)
                        ? airportCityElement.GetString() ?? city
                        : city;

                results.Add(
                    new FlightLocationSuggestion
                    {
                        Code = code,
                        AirportName = airportName,
                        City = airportCity,
                        Country = country
                    });
            }
        }

        IReadOnlyList<FlightLocationSuggestion> finalResults =
            results
                .GroupBy(
                    x => x.Code,
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .Take(10)
                .ToList();

        _cache.Set(
            cacheKey,
            finalResults,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromHours(1),

                Size = 1
            });

        return finalResults;
    }

    private static string ExtractCountry(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "";

        string[] parts = description
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 0 ? "" : parts[^1];
    }
}