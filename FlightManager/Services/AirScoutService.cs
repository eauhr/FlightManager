using System.Text.RegularExpressions;
using FlightManager.Models.AirScout;
using FlightManager.Services.ExternalFlights;

namespace FlightManager.Services;

public sealed class AirScoutService : IAirScoutService
{
    private static readonly IReadOnlyList<(string City, string Code, double X, double Y)> PulseCities =
    [
        ("New York", "JFK", 19, 34),
        ("Los Angeles", "LAX", 12, 43),
        ("Toronto", "YYZ", 22, 28),
        ("London", "LHR", 47, 30),
        ("Paris", "CDG", 49, 33),
        ("Dubai", "DXB", 62, 43),
        ("Tokyo", "HND", 82, 35),
        ("Singapore", "SIN", 75, 60),
        ("Sydney", "SYD", 86, 78),
        ("Sao Paulo", "GRU", 30, 72)
    ];

    private static readonly IReadOnlyList<(string From, string To)> PulseRoutes =
    [
        ("JFK", "HND"),
        ("LHR", "DXB"),
        ("LAX", "SIN"),
        ("CDG", "JFK"),
        ("GRU", "LHR"),
        ("YYZ", "DXB"),
        ("HND", "SYD"),
        ("JFK", "LAX"),
        ("LHR", "SIN"),
        ("LAX", "HND")
    ];

    private readonly IFlightSearchService _flightSearchService;

    public AirScoutService(IFlightSearchService flightSearchService)
    {
        _flightSearchService = flightSearchService;
    }

    public async Task<AirScoutHomeViewModel> BuildHomeViewAsync(
        CancellationToken cancellationToken = default)
    {
        return new AirScoutHomeViewModel
        {
            Request = new AirScoutSearchRequestViewModel(),
            Pulse = await BuildPulseAsync(cancellationToken)
        };
    }

    public async Task<AirScoutSearchResultsViewModel> SearchAsync(
        AirScoutSearchRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        AirScoutSearchRequestViewModel normalizedRequest = NormalizeRequest(request);

        var externalRequest = new ExternalFlightSearchRequest
        {
            DepartureAirport = normalizedRequest.Origin,
            ArrivalAirport = normalizedRequest.Destination,
            OutboundDate = normalizedRequest.DepartDate,
            ReturnDate = normalizedRequest.ReturnDate,
            ClientTimeZoneId = normalizedRequest.ClientTimeZoneId,
            Adults = normalizedRequest.Passengers,
            Children = 0,
            InfantsInSeat = 0,
            InfantsOnLap = 0,
            TravelClass = "economy",
            Stops = 0
        };

        IReadOnlyList<ExternalFlightOffer> offers = await _flightSearchService.SearchAsync(
            externalRequest,
            cancellationToken);

        List<AirScoutFlightOptionViewModel> flights = offers
            .Where(IsUsableLiveOffer)
            .Select(MapExternalOffer)
            .GroupBy(
                offer => $"{offer.FlightCode}:{offer.DepartureTime:O}:{offer.ArrivalTime:O}:{offer.Price}:{offer.CurrencyCode}",
                StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        flights = RankFlights(flights, normalizedRequest.Priority);

        for (int index = 0; index < flights.Count; index++)
        {
            flights[index].Rank = index + 1;
            flights[index].RankLabel = index == 0
                ? GetTopResultLabel(normalizedRequest.Priority)
                : string.Empty;
        }

        return new AirScoutSearchResultsViewModel
        {
            Request = normalizedRequest,
            InsightLine = flights.Count == 0
                ? $"No live flight options were found for {normalizedRequest.Origin} → {normalizedRequest.Destination}."
                : $"Found {flights.Count} live flight option{(flights.Count == 1 ? string.Empty : "s")} for {normalizedRequest.Origin} → {normalizedRequest.Destination}.",
            LiveFlightOptions = flights.Count,
            GeneratedAtUtc = DateTime.UtcNow,
            Flights = flights
        };
    }

    public Task<BookingRequest?> ResolveBookingRequestAsync(
        string bookingToken,
        CancellationToken cancellationToken = default)
    {
        return _flightSearchService.ResolveBookingRequestAsync(bookingToken, cancellationToken);
    }

    public Task<AirScoutBestOffersViewModel> GetBestOffersAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AirScoutBestOffersViewModel
        {
            GeneratedAtUtc = DateTime.UtcNow
        });
    }

    private static bool IsUsableLiveOffer(ExternalFlightOffer offer)
    {
        return offer.Price > 0
            && offer.DurationMinutes > 0
            && offer.Stops >= 0
            && !string.IsNullOrWhiteSpace(offer.Airline)
            && !string.IsNullOrWhiteSpace(offer.FlightNumbers)
            && !string.IsNullOrWhiteSpace(offer.Currency);
    }

    private static AirScoutFlightOptionViewModel MapExternalOffer(ExternalFlightOffer offer)
    {
        TimeSpan duration = TimeSpan.FromMinutes(offer.DurationMinutes);

        return new AirScoutFlightOptionViewModel
        {
            Airline = offer.Airline,
            FlightCode = offer.FlightNumbers,
            DepartureTime = offer.Departure,
            ArrivalTime = offer.Arrival,
            TotalTravelTime = duration,
            Stops = offer.Stops,
            LayoverAirport = offer.Stops == 0 ? "Nonstop" : "Not provided",
            CabinClass = "Economy",
            Price = offer.Price,
            CurrencyCode = offer.Currency,
            SourceName = "Google Flights",
            BookingToken = offer.BookingToken,
            BookingUrl = offer.BookingUrl ?? string.Empty,
            BookingPostData = offer.BookingPostData,
            ValueSummary = BuildValueSummary(offer)
        };
    }

    private static List<AirScoutFlightOptionViewModel> RankFlights(
        List<AirScoutFlightOptionViewModel> flights,
        AirScoutPriority priority)
    {
        if (flights.Count == 0)
        {
            return flights;
        }

        foreach (AirScoutFlightOptionViewModel flight in flights)
        {
            double priceScore = NormalizedLowerIsBetter(
                flights.Select(x => (double)x.Price),
                (double)flight.Price);
            double durationScore = NormalizedLowerIsBetter(
                flights.Select(x => x.TotalTravelTime.TotalMinutes),
                flight.TotalTravelTime.TotalMinutes);
            double stopScore = NormalizedLowerIsBetter(
                flights.Select(x => (double)x.Stops),
                flight.Stops);

            flight.AeroMetricScore = priority switch
            {
                AirScoutPriority.Cheapest => priceScore,
                AirScoutPriority.BestValue => priceScore * 0.55 + durationScore * 0.30 + stopScore * 0.15,
                AirScoutPriority.Premium => stopScore * 0.50 + durationScore * 0.35 + priceScore * 0.15,
                _ => priceScore
            };
        }

        IOrderedEnumerable<AirScoutFlightOptionViewModel> ranked = priority switch
        {
            AirScoutPriority.Cheapest => flights
                .OrderBy(flight => flight.Price)
                .ThenBy(flight => flight.TotalTravelTime)
                .ThenBy(flight => flight.Stops),
            AirScoutPriority.Premium => flights
                .OrderByDescending(flight => flight.AeroMetricScore)
                .ThenBy(flight => flight.Stops)
                .ThenBy(flight => flight.TotalTravelTime)
                .ThenBy(flight => flight.Price),
            _ => flights
                .OrderByDescending(flight => flight.AeroMetricScore)
                .ThenBy(flight => flight.Price)
                .ThenBy(flight => flight.TotalTravelTime)
                .ThenBy(flight => flight.Stops)
        };

        return ranked.ToList();
    }

    private static double NormalizedLowerIsBetter(IEnumerable<double> values, double value)
    {
        double[] sourceValues = values.ToArray();
        double minimum = sourceValues.Min();
        double maximum = sourceValues.Max();

        if (minimum == maximum)
        {
            return 100;
        }

        return Math.Clamp(
            100 - ((value - minimum) / (maximum - minimum) * 100),
            0,
            100);
    }

    private static string BuildValueSummary(ExternalFlightOffer offer)
    {
        string stopText = offer.Stops == 0
            ? "nonstop"
            : $"{offer.Stops} stop{(offer.Stops == 1 ? string.Empty : "s")}";

        return $"{FormatDuration(offer.DurationMinutes)} · {stopText}";
    }

    private static string GetTopResultLabel(AirScoutPriority priority)
    {
        return priority switch
        {
            AirScoutPriority.Cheapest => "Cheapest",
            AirScoutPriority.Premium => "Premium Pick",
            _ => "Best Value"
        };
    }

    private static string FormatDuration(int durationMinutes)
    {
        TimeSpan duration = TimeSpan.FromMinutes(durationMinutes);
        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }

    private async Task<AirScoutPulseViewModel> BuildPulseAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        List<AirScoutPulseRouteViewModel> routes = new();

        foreach ((string from, string to) in PulseRoutes)
        {
            int seed = StableSeed(from, to, today.ToString("yyyyMMdd"));
            int volume = 450 + seed % 720;
            int trend = -18 + seed % 43;
            (string heatKey, string heatLabel) = ResolveHeat(volume, trend);

            routes.Add(new AirScoutPulseRouteViewModel
            {
                FromCode = from,
                ToCode = to,
                SearchVolume = volume,
                TrendPercent = trend,
                HeatKey = heatKey,
                HeatLabel = heatLabel
            });
        }

        List<AirScoutPulseRouteViewModel> topRoutes = routes
            .OrderByDescending(route => route.SearchVolume)
            .Take(7)
            .ToList();

        int maxVolume = topRoutes.Max(route => route.SearchVolume);
        foreach (AirScoutPulseRouteViewModel route in topRoutes)
        {
            route.BarWidthPercent = Math.Max(
                18,
                (int)Math.Round(route.SearchVolume / (double)maxVolume * 100));
        }

        Dictionary<string, int> cityVolumes = new(StringComparer.OrdinalIgnoreCase);
        foreach (AirScoutPulseRouteViewModel route in topRoutes)
        {
            cityVolumes[route.FromCode] = cityVolumes.GetValueOrDefault(route.FromCode) + route.SearchVolume;
            cityVolumes[route.ToCode] = cityVolumes.GetValueOrDefault(route.ToCode) + route.SearchVolume;
        }

        int maxCityVolume = cityVolumes.Values.DefaultIfEmpty(1).Max();
        List<AirScoutPulseCityViewModel> bubbles = new();

        foreach ((string city, string code, double x, double y) in PulseCities)
        {
            int volume = cityVolumes.GetValueOrDefault(code);
            int routeTrend = topRoutes
                .Where(route => route.FromCode == code || route.ToCode == code)
                .Select(route => route.TrendPercent)
                .DefaultIfEmpty(0)
                .Max();
            (string heatKey, string heatLabel) = ResolveHeat(volume, routeTrend);

            bubbles.Add(new AirScoutPulseCityViewModel
            {
                City = city,
                AirportCode = code,
                SearchVolume = volume,
                BubbleSize = volume == 0
                    ? 12
                    : 14 + (int)Math.Round(volume / (double)maxCityVolume * 32),
                XPercent = x,
                YPercent = y,
                HeatKey = heatKey,
                HeatLabel = heatLabel
            });
        }

        return new AirScoutPulseViewModel
        {
            LastUpdatedUtc = DateTime.UtcNow,
            CityBubbles = bubbles,
            TopRoutes = topRoutes
        };
    }

    private static (string HeatKey, string HeatLabel) ResolveHeat(int volume, int trendPercent)
    {
        if (trendPercent < -4)
        {
            return ("cooling", "Cooling Off");
        }

        if (volume >= 900)
        {
            return ("high", "High Demand");
        }

        if (trendPercent >= 9)
        {
            return ("rising", "Rising");
        }

        return ("moderate", "Moderate");
    }

    private static int StableSeed(params string[] values)
    {
        int hash = string.Join('|', values)
            .Aggregate(17, (current, value) => unchecked(current * 31 + value));

        return hash & int.MaxValue;
    }

    private static AirScoutSearchRequestViewModel NormalizeRequest(AirScoutSearchRequestViewModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string origin = NormalizeAirportCode(request.Origin, "Origin");
        string destination = NormalizeAirportCode(request.Destination, "Destination");

        if (origin == destination)
        {
            throw new ArgumentException("Origin and destination must be different airports.");
        }

        DateOnly today = TravelerLocalDate.Today(request.ClientTimeZoneId);
        if (request.DepartDate < today)
        {
            throw new ArgumentException("Departure date cannot be in the past.");
        }

        if (request.ReturnDate.HasValue && request.ReturnDate.Value < request.DepartDate)
        {
            throw new ArgumentException("Return date cannot be before the departure date.");
        }

        if (request.Passengers is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Passengers), "Passengers must be between 1 and 9.");
        }

        return new AirScoutSearchRequestViewModel
        {
            Origin = origin,
            Destination = destination,
            DepartDate = request.DepartDate,
            ReturnDate = request.ReturnDate,
            Passengers = request.Passengers,
            Priority = request.Priority,
            ClientTimeZoneId = request.ClientTimeZoneId
        };
    }

    private static string NormalizeAirportCode(string? value, string fieldName)
    {
        string code = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!Regex.IsMatch(code, "^[A-Z]{3}$"))
        {
            throw new ArgumentException($"{fieldName} must be a valid IATA airport code.", fieldName);
        }

        return code;
    }
}
