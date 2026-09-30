using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models.AirScout;

public enum AirScoutPriority
{
    Cheapest = 0,
    BestValue = 1,
    Premium = 2
}

public static class TravelerLocalDate
{
    public static DateOnly Today(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
            catch (ArgumentException)
            {
            }
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

public static class CurrencyHelper
{
    public static string GetCurrencyCode(string? airportCode) => airportCode?.ToUpperInvariant() switch
    {
        "JFK" or "LAX" or "ORD" or "ATL" or "SFO" or "MIA" or "DFW" or "DEN" => "USD",
        "LHR" or "MAN" or "EDI" or "GLA" => "GBP",
        "HND" or "NRT" => "JPY",
        "SOF" or "VAR" => "BGN",
        "WAW" or "KRK" => "PLN",
        "PRG" => "CZK",
        "BUD" => "HUF",
        "IST" => "TRY",
        _ => "EUR"
    };

    public static string GetCurrencySymbol(string? code) => code?.ToUpperInvariant() switch
    {
        "USD" => "$",
        "GBP" => "£",
        "EUR" => "€",
        "JPY" => "¥",
        "BGN" => "лв",
        "PLN" => "zł",
        "CZK" => "Kč",
        "HUF" => "Ft",
        "TRY" => "₺",
        _ => "€"
    };

    public static string FormatPrice(decimal amount, string? currencyCode)
    {
        return $"{GetCurrencySymbol(currencyCode)}{amount:N0}";
    }
}

public sealed class AirScoutSearchRequestViewModel
{
    [StringLength(100)]
    public string? ClientTimeZoneId { get; set; }

    [Required]
    [StringLength(64)]
    public string Origin { get; set; } = "JFK";

    [Required]
    [StringLength(64)]
    public string Destination { get; set; } = "LHR";

    [Required]
    public DateOnly DepartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));

    public DateOnly? ReturnDate { get; set; }

    [Range(1, 9)]
    public int Passengers { get; set; } = 1;

    public AirScoutPriority Priority { get; set; } = AirScoutPriority.BestValue;
}

public sealed class AirScoutFlightOptionViewModel
{
    public int Rank { get; set; }

    public string RankLabel { get; set; } = string.Empty;

    public string Airline { get; set; } = string.Empty;

    public string FlightCode { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public TimeSpan TotalTravelTime { get; set; }

    public int Stops { get; set; }

    public string LayoverAirport { get; set; } = "Not provided";

    public string CabinClass { get; set; } = "Economy";

    public decimal Price { get; set; }

    public string CurrencyCode { get; set; } = "EUR";

    public string FormattedPrice => CurrencyHelper.FormatPrice(Price, CurrencyCode);

    public string SourceName { get; set; } = "Google Flights";

    public string? BookingToken { get; set; }

    public string BookingUrl { get; set; } = string.Empty;

    public string? BookingPostData { get; set; }

    public string ValueSummary { get; set; } = string.Empty;

    public double AeroMetricScore { get; set; }
}

public sealed class AirScoutPulseCityViewModel
{
    public string City { get; set; } = string.Empty;
    public string AirportCode { get; set; } = string.Empty;
    public int SearchVolume { get; set; }
    public int BubbleSize { get; set; }
    public double XPercent { get; set; }
    public double YPercent { get; set; }
    public string HeatKey { get; set; } = "moderate";
    public string HeatLabel { get; set; } = "Moderate";
}

public sealed class AirScoutPulseRouteViewModel
{
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public int SearchVolume { get; set; }
    public int TrendPercent { get; set; }
    public int BarWidthPercent { get; set; }
    public string HeatKey { get; set; } = "moderate";
    public string HeatLabel { get; set; } = "Moderate";
    public string RouteLabel => $"{FromCode} -> {ToCode}";
}

public sealed class AirScoutPulseViewModel
{
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<AirScoutPulseCityViewModel> CityBubbles { get; set; } = new();
    public List<AirScoutPulseRouteViewModel> TopRoutes { get; set; } = new();
}

public sealed class AirScoutHomeViewModel
{
    public AirScoutSearchRequestViewModel Request { get; set; } = new();
    public AirScoutPulseViewModel Pulse { get; set; } = new();
}

public sealed class AirScoutSearchResultsViewModel
{
    public AirScoutSearchRequestViewModel Request { get; set; } = new();
    public string InsightLine { get; set; } = string.Empty;
    public int LiveFlightOptions { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<AirScoutFlightOptionViewModel> Flights { get; set; } = new();
    public decimal LowestPrice => Flights.Count == 0 ? 0 : Flights.Min(flight => flight.Price);
}

public sealed class AirScoutBestOfferRoute
{
    public string Origin { get; set; } = string.Empty;
    public string OriginName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public int DiscountPercent { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public DateOnly DepartureDate { get; set; }
    public string Airline { get; set; } = string.Empty;
    public string CabinClass { get; set; } = "Economy";
    public int DurationHours { get; set; }
    public string BookingUrl { get; set; } = string.Empty;
    public string FormattedPrice => CurrencyHelper.FormatPrice(DiscountedPrice, CurrencyCode);
    public string OriginalFormattedPrice => CurrencyHelper.FormatPrice(OriginalPrice, CurrencyCode);
    public string RouteLabel => $"{Origin} → {Destination}";
}

public sealed class AirScoutBestOffersViewModel
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<AirScoutBestOfferRoute> Offers { get; set; } = new();
    public int TotalOffers => Offers.Count;
    public double AverageDiscount => Offers.Count == 0 ? 0 : Offers.Average(offer => offer.DiscountPercent);
}
