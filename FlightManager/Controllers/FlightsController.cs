using FlightManager.Data;
using FlightManager.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightManager.Controllers
{
    public class FlightsController : Controller
    {
        private readonly FlightContext flightcontext;

        public FlightsController(FlightContext flightcontext)
        {
            this.flightcontext = flightcontext;
        }

        // GET: Flights
        [Authorize]
        public async Task<IActionResult> Index(string? from, string? to, int page = 1, int pageSize = 10)
        {
            IEnumerable<Flight> flights = await flightcontext.GetFilteredAsync(from, to, page, pageSize);
            int total = await flightcontext.GetCountAsync(from, to);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.From = from;
            ViewBag.To = to;

            return View(flights);
        }

        // GET: Flights/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id, int page = 1)
        {
            if (id == null) return NotFound();

            Flight flight = await flightcontext.ReadAsync(id.Value);
            if (flight == null) return NotFound();

            var passengers = flight.Reservations
                .Where(r => r.IsConfirmed)
                .SelectMany(r => r.Passengers)
                .ToList();

            int pageSize = 10;
            int totalPages = (int)Math.Ceiling(passengers.Count / (double)pageSize);
            var paged = passengers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Passengers = paged;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Duration = flight.ArrivalTime - flight.DepartureTime;

            return View(flight);
        }

        // GET: Flights/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Flight());
        }

        // POST: Flights/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DepartureCity,ArrivalCity,DepartureTime,ArrivalTime,PlaneType,PlaneNumber,PilotName,PassengersCapacity,BusinessClassCapacity")] Flight flight)
        {
            if (flight.ArrivalTime <= flight.DepartureTime)
                ModelState.AddModelError("ArrivalTime", "Arrival time must be after departure time.");

            if (!ModelState.IsValid) 
                return View(flight);

            await flightcontext.CreateAsync(flight);
            return RedirectToAction(nameof(Index));
        }

        // GET: Flights/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var flight = await flightcontext.ReadAsync(id.Value);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flights/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DepartureCity,ArrivalCity,DepartureTime,ArrivalTime,PlaneType,PlaneNumber,PilotName,PassengersCapacity,BusinessClassCapacity")] Flight flight)
        {
            if (id != flight.Id) return NotFound();

            if (flight.ArrivalTime <= flight.DepartureTime)
                ModelState.AddModelError("ArrivalTime", "Arrival time must be after departure time.");

            if (!ModelState.IsValid) return View(flight);

            await flightcontext.UpdateAsync(flight);
            return RedirectToAction(nameof(Index));
        }

        // GET: Flights/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var flight = await flightcontext.ReadAsync(id.Value);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flights/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await flightcontext.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
