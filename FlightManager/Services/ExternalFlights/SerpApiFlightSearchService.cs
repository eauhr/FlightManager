using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlightManager.Models.AirScout;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FlightManager.Services.ExternalFlights;

public sealed class FlightSearchUnavailableException : Exception
{
    public FlightSearchUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

public sealed class SerpApiFlightSearchService : IFlightSearchService
{
    private static readonly Regex AirportCodePattern = new("^[A-Z]{3}$", RegexOptions.Compiled);
    private readonly HttpClient _http;
    private readonly FlightSearchOptions _options;
    private readonly IMemoryCache _cache;

    public SerpApiFlightSearchService(HttpClient http, IOptions<FlightSearchOptions> options, IMemoryCache cache)
    {
        _http = http;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ExternalFlightOffer>> SearchAsync(ExternalFlightSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);
        EnsureConfiguration();

        string origin = request.DepartureAirport.Trim().ToUpperInvariant();
        string destination = request.ArrivalAirport.Trim().ToUpperInvariant();
        string cacheKey = CreateSearchCacheKey(request, origin, destination);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ExternalFlightOffer>? cached) && cached is not null)
            return cached;

        string url = BuildSearchUrl(request, origin, destination);
        string body = await GetJsonWithRetryAsync(url, "flight search", cancellationToken);

        using JsonDocument document = ParseResponse(body, "flight search");
        List<ExternalFlightOffer> offers = [];
        AddOffers(document.RootElement, "best_flights", offers);
        AddOffers(document.RootElement, "other_flights", offers);

        IReadOnlyList<ExternalFlightOffer> finalOffers = offers
            .Where(offer => offer.Origin.Equals(origin, StringComparison.OrdinalIgnoreCase)
                         && offer.Destination.Equals(destination, StringComparison.OrdinalIgnoreCase))
            .Where(IsUsableOffer)
            .GroupBy(offer => $"{offer.FlightNumbers}|{offer.Departure:O}|{offer.Arrival:O}|{offer.Price}|{offer.Currency}", StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(offer => offer.Price)
            .ThenBy(offer => offer.DurationMinutes)
            .ToList();

        _cache.Set(cacheKey, finalOffers, TimeSpan.FromSeconds(_options.SearchCacheSeconds));

        string requestType = request.RoundTrip ? "1" : "2";
        string? returnDate = request.RoundTrip
            ? request.ReturnDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
        string outboundDate = request.OutboundDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        foreach (ExternalFlightOffer offer in finalOffers)
        {
            if (string.IsNullOrWhiteSpace(offer.BookingToken)) continue;
            _cache.Set(
                BookingContextCacheKey(offer.BookingToken),
                new BookingContext(origin, destination, outboundDate, returnDate, requestType),
                TimeSpan.FromMinutes(30));
        }

        return finalOffers;
    }

    private static string BookingContextCacheKey(string bookingToken) => $"booking-ctx:{bookingToken}";

    private sealed record BookingContext(
        string DepartureId,
        string ArrivalId,
        string OutboundDate,
        string? ReturnDate,
        string Type);

    public async Task<BookingRequest?> ResolveBookingRequestAsync(string bookingToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookingToken)) return null;
        EnsureConfiguration();

        if (!_cache.TryGetValue(BookingContextCacheKey(bookingToken), out BookingContext? context) || context is null)
        {
            throw new FlightSearchUnavailableException(
                "This flight offer has expired. Please search again and select a fresh result.");
        }

        List<KeyValuePair<string, string>> query =
        [
            new("engine", "google_flights"),
            new("api_key", _options.ApiKey),
            new("departure_id", context.DepartureId),
            new("arrival_id", context.ArrivalId),
            new("outbound_date", context.OutboundDate),
            new("type", context.Type),
            new("currency", _options.Currency),
            new("gl", _options.Country),
            new("hl", _options.Language),
            new("booking_token", bookingToken)
        ];
        if (context.ReturnDate is not null)
        {
            query.Add(new("return_date", context.ReturnDate));
        }

        string url = BuildUrl(query);
        string body = await GetJsonWithRetryAsync(url, "booking resolution", cancellationToken);
        using JsonDocument document = ParseResponse(body, "booking resolution");

        if (!document.RootElement.TryGetProperty("booking_options", out JsonElement options)
            || options.ValueKind != JsonValueKind.Array)
            return null;

        BookingRequest? firstValidRequest = null;
        BookingRequest? lowestPricedRequest = null;
        decimal lowestPrice = decimal.MaxValue;

        foreach (JsonElement option in options.EnumerateArray())
        {
            if (!option.TryGetProperty("together", out JsonElement together)
                || !together.TryGetProperty("booking_request", out JsonElement request)
                || !request.TryGetProperty("url", out JsonElement urlElement)) continue;

            string? bookingUrl = urlElement.GetString();
            if (!Uri.TryCreate(bookingUrl, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) continue;

            BookingRequest candidate = new()
            {
                Url = uri.ToString(),
                PostData = request.TryGetProperty("post_data", out JsonElement postData) ? postData.GetString() : null
            };
            firstValidRequest ??= candidate;

            if (together.TryGetProperty("price", out JsonElement priceElement)
                && priceElement.TryGetDecimal(out decimal price)
                && price >= 0
                && price < lowestPrice)
            {
                lowestPrice = price;
                lowestPricedRequest = candidate;
            }
        }

        return lowestPricedRequest ?? firstValidRequest;
    }

    private void AddOffers(JsonElement root, string propertyName, List<ExternalFlightOffer> output)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement rawOffers) || rawOffers.ValueKind != JsonValueKind.Array) return;
        foreach (JsonElement rawOffer in rawOffers.EnumerateArray())
        {
            if (TryParseOffer(rawOffer, out ExternalFlightOffer? offer) && offer is not null)
                output.Add(offer);
        }
    }

    private bool TryParseOffer(JsonElement rawOffer, out ExternalFlightOffer? offer)
    {
        offer = null;
        if (!rawOffer.TryGetProperty("flights", out JsonElement legs) || legs.ValueKind != JsonValueKind.Array || legs.GetArrayLength() == 0) return false;
        JsonElement first = legs[0];
        JsonElement last = legs[legs.GetArrayLength() - 1];
        if (!first.TryGetProperty("departure_airport", out JsonElement departureAirport)
            || !last.TryGetProperty("arrival_airport", out JsonElement arrivalAirport)) return false;

        string origin = GetString(departureAirport, "id").ToUpperInvariant();
        string destination = GetString(arrivalAirport, "id").ToUpperInvariant();
        if (!IsAirportCode(origin) || !IsAirportCode(destination)) return false;
        if (!DateTime.TryParse(GetString(departureAirport, "time"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime departure)
            || !DateTime.TryParse(GetString(arrivalAirport, "time"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime arrival)) return false;
        if (!rawOffer.TryGetProperty("price", out JsonElement priceElement) || !priceElement.TryGetDecimal(out decimal price) || price <= 0) return false;

        int duration = rawOffer.TryGetProperty("total_duration", out JsonElement durationElement) && durationElement.TryGetInt32(out int apiDuration)
            ? apiDuration : (int)(arrival - departure).TotalMinutes;
        if (duration <= 0) return false;

        List<string> flightNumbers = legs.EnumerateArray().Select(leg => GetString(leg, "flight_number"))
            .Where(number => !string.IsNullOrWhiteSpace(number)).ToList();
        List<string> airlines = legs.EnumerateArray().Select(leg => GetString(leg, "airline"))
            .Where(airline => !string.IsNullOrWhiteSpace(airline)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (flightNumbers.Count == 0 || airlines.Count == 0) return false;

        offer = new ExternalFlightOffer
        {
            Airline = string.Join(" / ", airlines),
            FlightNumbers = string.Join(" / ", flightNumbers),
            Origin = origin,
            Destination = destination,
            Departure = departure,
            Arrival = arrival,
            Stops = Math.Max(0, legs.GetArrayLength() - 1),
            DurationMinutes = duration,
            Price = price,
            Currency = _options.Currency,
            BookingToken = GetStringOrNull(rawOffer, "booking_token"),
            LogoUrl = GetStringOrNull(rawOffer, "airline_logo")
        };
        return true;
    }

    private async Task<string> GetJsonWithRetryAsync(string url, string operation, CancellationToken cancellationToken)
    {
        const int attempts = 2;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await _http.GetAsync(url, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode) return body;
                if (attempt < attempts && IsTransient(response.StatusCode))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
                    continue;
                }
                throw new FlightSearchUnavailableException($"Live {operation} is temporarily unavailable.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (TaskCanceledException exception)
            {
                if (attempt < attempts) { await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken); continue; }
                throw new FlightSearchUnavailableException($"Live {operation} timed out.", exception);
            }
            catch (HttpRequestException exception)
            {
                if (attempt < attempts) { await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken); continue; }
                throw new FlightSearchUnavailableException($"Live {operation} is temporarily unavailable.", exception);
            }
        }
    }

    private static JsonDocument ParseResponse(string body, string operation)
    {
        try
        {
            JsonDocument document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out JsonElement error))
            {
                document.Dispose();
                throw new FlightSearchUnavailableException($"Live {operation} is temporarily unavailable.");
            }
            return document;
        }
        catch (JsonException exception) { throw new FlightSearchUnavailableException($"Live {operation} returned an invalid response.", exception); }
    }

    private string BuildSearchUrl(ExternalFlightSearchRequest request, string origin, string destination)
    {
        List<KeyValuePair<string, string>> query =
        [
            new("engine", "google_flights"), new("api_key", _options.ApiKey), new("departure_id", origin), new("arrival_id", destination),
            new("outbound_date", request.OutboundDate.ToString("yyyy-MM-dd")), new("type", request.RoundTrip ? "1" : "2"),
            new("travel_class", TravelClassToSerp(request.TravelClass)), new("adults", request.Adults.ToString(CultureInfo.InvariantCulture)),
            new("children", request.Children.ToString(CultureInfo.InvariantCulture)), new("infants_in_seat", request.InfantsInSeat.ToString(CultureInfo.InvariantCulture)),
            new("infants_on_lap", request.InfantsOnLap.ToString(CultureInfo.InvariantCulture)), new("currency", _options.Currency),
            new("gl", _options.Country), new("hl", _options.Language)
        ];
        if (_options.DeepSearch) query.Add(new("deep_search", "true"));
        if (request.RoundTrip) query.Add(new("return_date", request.ReturnDate!.Value.ToString("yyyy-MM-dd")));
        if (request.Stops > 0) query.Add(new("stops", request.Stops.ToString(CultureInfo.InvariantCulture)));
        return BuildUrl(query);
    }

    private string BuildUrl(IEnumerable<KeyValuePair<string, string>> query) => _options.BaseUrl + "?" + string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    private string CreateSearchCacheKey(ExternalFlightSearchRequest request, string origin, string destination) => $"flight-search:{origin}:{destination}:{request.OutboundDate:yyyyMMdd}:{request.ReturnDate:yyyyMMdd}:{request.Adults}:{request.Children}:{request.InfantsInSeat}:{request.InfantsOnLap}:{request.TravelClass}:{request.Stops}:{_options.Currency}";
    private void EnsureConfiguration() { if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new FlightSearchUnavailableException("Live flight search is not configured."); }
    private static bool IsAirportCode(string value) => AirportCodePattern.IsMatch(value);
    private static bool IsUsableOffer(ExternalFlightOffer offer) => offer.Price > 0 && offer.DurationMinutes > 0 && offer.Stops >= 0 && IsAirportCode(offer.Origin) && IsAirportCode(offer.Destination);
    private static bool IsTransient(HttpStatusCode status) => status == HttpStatusCode.RequestTimeout || status == (HttpStatusCode)429 || (int)status >= 500;
    private static string GetString(JsonElement element, string property) => element.TryGetProperty(property, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;
    private static string? GetStringOrNull(JsonElement element, string property) => element.TryGetProperty(property, out JsonElement value) ? value.GetString() : null;
    private static string TravelClassToSerp(string? travelClass) => travelClass?.Trim().ToLowerInvariant() switch { "premium" or "premium economy" => "2", "business" => "3", "first" => "4", _ => "1" };

    private static void ValidateRequest(ExternalFlightSearchRequest request)
    {
        string origin = request.DepartureAirport?.Trim().ToUpperInvariant() ?? string.Empty;
        string destination = request.ArrivalAirport?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!IsAirportCode(origin) || !IsAirportCode(destination)) throw new ArgumentException("Origin and destination must be valid IATA airport codes.");
        if (origin == destination) throw new ArgumentException("Origin and destination must be different airports.");
        DateOnly today = TravelerLocalDate.Today(request.ClientTimeZoneId);
        if (request.OutboundDate < today) throw new ArgumentException("Outbound date cannot be in the past.");
        if (request.ReturnDate is { } returnDate && returnDate < request.OutboundDate) throw new ArgumentException("Return date cannot be before the outbound date.");
        if (request.Adults is < 1 or > 9 || request.Children is < 0 or > 8 || request.InfantsInSeat is < 0 or > 8 || request.InfantsOnLap is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(request), "Passenger counts are invalid.");
        if (request.Stops is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(request.Stops), "Stops must be between 0 and 3.");
    }
}