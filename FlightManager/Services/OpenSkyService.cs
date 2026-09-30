using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightManager.Services;

public interface IAviationDataService
{
    Task<OpenSkyStateResponse?> GetAirspaceAsync(double? minLat = null, double? maxLat = null, double? minLon = null, double? maxLon = null, CancellationToken cancellationToken = default);
    Task<List<OpenSkyAircraft>> GetAircraftByRegionAsync(double latMin, double lonMin, double latMax, double lonMax, CancellationToken cancellationToken = default);
    Task<AirspaceStats> GetGlobalAirspaceStatsAsync(CancellationToken cancellationToken = default);
}

public sealed class OpenSkyService : IAviationDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenSkyService> _logger;

    public OpenSkyService(HttpClient httpClient, ILogger<OpenSkyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OpenSkyStateResponse?> GetAirspaceAsync(
        double? minLat = null,
        double? maxLat = null,
        double? minLon = null,
        double? maxLon = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string url = "states/all";
            if (minLat.HasValue || maxLat.HasValue || minLon.HasValue || maxLon.HasValue)
            {
                var queryParams = new List<string>();
                if (minLat.HasValue) queryParams.Add($"lamin={minLat}");
                if (maxLat.HasValue) queryParams.Add($"lamax={maxLat}");
                if (minLon.HasValue) queryParams.Add($"lomin={minLon}");
                if (maxLon.HasValue) queryParams.Add($"lomax={maxLon}");
                url += "?" + string.Join("&", queryParams);
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OpenSkyStateResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.StatesArray != null)
            {
                result.States = result.StatesArray
                    .Select(arr => OpenSkyAircraft.FromStateVector(arr))
                    .Where(a => a.Latitude.HasValue && a.Longitude.HasValue)
                    .ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch airspace data from OpenSky API");
            return null;
        }
    }

    public async Task<List<OpenSkyAircraft>> GetAircraftByRegionAsync(
        double latMin,
        double lonMin,
        double latMax,
        double lonMax,
        CancellationToken cancellationToken = default)
    {
        var response = await GetAirspaceAsync(latMin, latMax, lonMin, lonMax, cancellationToken);
        return response?.States ?? [];
    }

    public async Task<AirspaceStats> GetGlobalAirspaceStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var bounds = new (string Region, double MinLat, double MaxLat, double MinLon, double MaxLon)[]
            {
                ("North America", 25, 60, -130, -65),
                ("Europe", 35, 60, -15, 30),
                ("Asia", 10, 55, 60, 145),
                ("Oceania", -50, -10, 110, 180),
                ("South America", -55, 10, -80, -35),
                ("Africa", -35, 35, -20, 55)
            };

            var stats = new AirspaceStats { LastUpdatedUtc = DateTime.UtcNow };

            foreach (var (region, minLat, maxLat, minLon, maxLon) in bounds)
            {
                var aircraft = await GetAircraftByRegionAsync(minLat, minLon, maxLat, maxLon, cancellationToken);
                stats.RegionCounts[region] = aircraft.Count;
            }

            var total = await GetAirspaceAsync(cancellationToken: cancellationToken);
            stats.TotalAircraft = total?.States?.Count ?? 0;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch global airspace stats");
            return new AirspaceStats { LastUpdatedUtc = DateTime.UtcNow };
        }
    }
}

public class OpenSkyStateResponse
{
    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("states")]
    public string[][]? StatesArray { get; set; }

    [JsonIgnore]
    public List<OpenSkyAircraft>? States { get; set; }
}

public class OpenSkyAircraft
{
    [JsonPropertyName("icao24")]
    public string Icao24 { get; set; } = string.Empty;

    [JsonPropertyName("callsign")]
    public string? CallSign { get; set; }

    [JsonPropertyName("origin_country")]
    public string? OriginCountry { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("geo_altitude")]
    public double? Altitude { get; set; }

    [JsonPropertyName("ground_speed")]
    public double? GroundSpeed { get; set; }

    [JsonPropertyName("true_track")]
    public double? TrueTrack { get; set; }

    [JsonPropertyName("last_contact")]
    public string? LastSeen { get; set; }

    public static OpenSkyAircraft FromStateVector(string[] data)
    {
        if (data.Length < 10)
            return new OpenSkyAircraft();

        return new OpenSkyAircraft
        {
            Icao24 = data[0] ?? string.Empty,
            CallSign = data.Length > 2 ? data[2]?.Trim() : null,
            OriginCountry = data.Length > 3 ? data[3] : null,
            Longitude = data.Length > 5 && double.TryParse(data[5], out var lon) ? lon : null,
            Latitude = data.Length > 6 && double.TryParse(data[6], out var lat) ? lat : null,
            Altitude = data.Length > 7 && double.TryParse(data[7], out var alt) ? alt : null,
            GroundSpeed = data.Length > 9 && double.TryParse(data[9], out var gs) ? gs : null,
            TrueTrack = data.Length > 10 && double.TryParse(data[10], out var tt) ? tt : null
        };
    }
}

public class AirspaceStats
{
    public DateTime LastUpdatedUtc { get; set; }
    public int TotalAircraft { get; set; }
    public Dictionary<string, int> RegionCounts { get; set; } = new();
}