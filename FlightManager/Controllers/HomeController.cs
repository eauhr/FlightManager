using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FlightManager.Models;
using FlightManager.Models.AirScout;
using FlightManager.Services;
using FlightManager.Services.ExternalFlights;
using Microsoft.AspNetCore.Mvc;

namespace FlightManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAirScoutService _airScoutService;
        private readonly IAviationDataService aviationDataService;
        private readonly IFlightLocationSearchService _flightLocationSearchService;

        public HomeController(
            ILogger<HomeController> logger,
            IAirScoutService airScoutService,
            IAviationDataService aviationDataService,
            IFlightLocationSearchService flightLocationSearchService)
        {
            _logger = logger;
            _airScoutService = airScoutService;
            this.aviationDataService = aviationDataService;
            _flightLocationSearchService = flightLocationSearchService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            AirScoutHomeViewModel model =
                await _airScoutService.BuildHomeViewAsync(cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] AirScoutSearchRequestViewModel request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid ||
                !IsIataCode(request.Origin) ||
                !IsIataCode(request.Destination) ||
                request.Origin.Equals(request.Destination, StringComparison.OrdinalIgnoreCase) ||
                request.Passengers is < 1 or > 9 ||
                request.DepartDate < TravelerLocalDate.Today(request.ClientTimeZoneId) ||
                (request.ReturnDate.HasValue && request.ReturnDate.Value < request.DepartDate))
            {
                TempData["Error"] =
                    "Please select valid airports and travel dates from the search form.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                AirScoutSearchResultsViewModel model =
                    await _airScoutService.SearchAsync(request, cancellationToken);
                return View(model);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (ArgumentException)
            {
                TempData["Error"] = "Please select valid airports, travel dates, and passenger counts.";
                return RedirectToAction(nameof(Index));
            }
            catch (FlightSearchUnavailableException ex)
            {
                _logger.LogWarning(ex, "Flight search failed for {Origin} to {Destination}", request.Origin, request.Destination);
                TempData["Error"] = ex.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase)
                    ? "Live flight search is not configured yet. Please contact the site administrator."
                    : "Flight search is temporarily unavailable. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Flight search provider request failed for {Origin} to {Destination}", request.Origin, request.Destination);
                TempData["Error"] = "Flight search is temporarily unavailable. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> AirportSuggestions(
            [FromQuery] string q,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(Array.Empty<FlightLocationSuggestion>());

            try
            {
                IReadOnlyList<FlightLocationSuggestion> suggestions =
                    await _flightLocationSearchService.SearchAsync(q, cancellationToken);
                return Json(suggestions);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Airport suggestion lookup failed for query '{Query}'", q);
                return Json(Array.Empty<FlightLocationSuggestion>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> LocationSuggestions(
    string query,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query) ||
                query.Trim().Length < 2)
            {
                return Json(Array.Empty<object>());
            }

            try
            {
                IReadOnlyList<FlightLocationSuggestion> results =
                    await _flightLocationSearchService.SearchAsync(
                        query,
                        cancellationToken);

                return Json(results);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Location suggestion lookup failed for query '{Query}'", query);
                return Json(Array.Empty<object>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(
            [FromForm] string bookingToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bookingToken))
            {
                TempData["Error"] = "Booking is currently unavailable for this flight.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                BookingRequest? bookingRequest = await _airScoutService.ResolveBookingRequestAsync(
                    bookingToken,
                    cancellationToken);

                if (bookingRequest is null || string.IsNullOrWhiteSpace(bookingRequest.Url))
                {
                    TempData["Error"] = "Booking is currently unavailable for this flight.";
                    return RedirectToAction(nameof(Index));
                }

                if (!Uri.TryCreate(bookingRequest.Url, UriKind.Absolute, out Uri? bookingUri) ||
                    bookingUri.Scheme != Uri.UriSchemeHttps)
                {
                    TempData["Error"] = "The booking provider returned an invalid link.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(bookingRequest.PostData))
                {
                    return Redirect(bookingUri.AbsoluteUri);
                }

                if (!IsGoogleBookingEndpoint(bookingUri))
                {
                    TempData["Error"] = "The booking provider returned an unsupported handoff.";
                    return RedirectToAction(nameof(Index));
                }

                Response.Headers.CacheControl = "no-store";
                return Content(BuildBookingHandoffHtml(bookingUri, bookingRequest.PostData), "text/html; charset=utf-8");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (FlightSearchUnavailableException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "The booking provider is temporarily unavailable. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TempData["Error"] = "The booking provider took too long to respond. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private static bool IsGoogleBookingEndpoint(Uri uri) =>
            uri.Host.Equals("www.google.com", StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath.Equals("/travel/clk/f", StringComparison.OrdinalIgnoreCase);

        private static string BuildBookingHandoffHtml(Uri actionUri, string postData)
        {
            StringBuilder html = new();
            string encodedAction = HtmlEncoder.Default.Encode(actionUri.AbsoluteUri);
            html.Append("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Opening booking provider</title>");
            html.Append("<style>body{font:16px system-ui,sans-serif;color:#171b36;background:#f4f8fb;display:grid;min-height:100vh;place-items:center;margin:0}.card{max-width:28rem;margin:1rem;padding:2rem;border:1px solid #e1e9ef;border-radius:1rem;background:white;box-shadow:0 16px 40px #15354d12}button{padding:.8rem 1.2rem;border:0;border-radius:.55rem;background:#1684d4;color:white;font-weight:700;cursor:pointer}</style><main class=\"card\"><h1>Opening booking provider</h1><p>Continue to the provider to review this flight and complete your booking.</p><form id=\"handoff\" method=\"post\" action=\"");
            html.Append(encodedAction).Append("\">");

            foreach (string part in postData.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = part.IndexOf('=');
                string name = WebUtility.UrlDecode(separator < 0 ? part : part[..separator]);
                string value = WebUtility.UrlDecode(separator < 0 ? string.Empty : part[(separator + 1)..]);
                if (string.IsNullOrEmpty(name)) continue;
                html.Append("<input type=\"hidden\" name=\"")
                    .Append(HtmlEncoder.Default.Encode(name))
                    .Append("\" value=\"")
                    .Append(HtmlEncoder.Default.Encode(value))
                    .Append("\">");
            }

            html.Append("<button type=\"submit\">Continue to booking</button></form></main><script>document.getElementById('handoff').submit();</script></html>");
            return html.ToString();
        }


        [HttpGet]
        public async Task<IActionResult> BestOffers(CancellationToken cancellationToken)
        {
            AirScoutBestOffersViewModel model =
                await _airScoutService.GetBestOffersAsync(cancellationToken);
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private static bool IsIataCode(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            Regex.IsMatch(value.Trim(), "^[A-Za-z]{3}$");
    }


}
