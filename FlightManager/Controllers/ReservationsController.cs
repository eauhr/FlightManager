using FlightManager.Data;
using FlightManager.Models;
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
    public class ReservationsController : Controller
    {
        private readonly ReservationContext reservationContext;
        private readonly FlightContext flightContext;

        public ReservationsController(ReservationContext reservationContext, FlightContext flightContext)
        {
            this.reservationContext = reservationContext;
            this.flightContext = flightContext;
        }

        // GET: Reservations
        public async Task<IActionResult> Index(string? email, int page = 1, int pageSize = 10)
        {
            IEnumerable<Reservation> reservations = await reservationContext.GetFilteredAsync(email, page, pageSize);
            int total = await reservationContext.GetCountAsync(email);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Email = email;

            return View(reservations);
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            Reservation reservation = await reservationContext.ReadAsync(id.Value);
            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create(int flightId)
        {
            Flight flight = await flightContext.ReadAsync(flightId);
            if (flight == null) return NotFound();

            ViewBag.Flight = flight;
            ViewBag.FlightId = flightId;
            return View(new Reservation { FlightId = flightId });
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ContactEmail,IsConfirmed,FlightId")] Reservation reservation, List<Passenger> passengers)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Flight = await flightContext.ReadAsync(reservation.FlightId);
                return View(reservation);
            }

            if (!passengers.Any())
            {
                ModelState.AddModelError("", "At least one passenger is required.");
                ViewBag.Flight = await flightContext.ReadAsync(reservation.FlightId);
                return View(reservation);
            }

            Flight flight = await flightContext.ReadAsync(reservation.FlightId);
            if (flight == null) return NotFound();

            reservation.IsConfirmed = true;
            reservation.Passengers = passengers;

            string? error = await reservationContext.TryCreateAsync(
                reservation,
                flight.PassengersCapacity,
                flight.BusinessClassCapacity);

            if (error != null)
            {
                ModelState.AddModelError("", error);
                ViewBag.Flight = flight;
                return View(reservation);
            }

            await SendConfirmationEmailAsync(reservation, flight);

            TempData["Success"] = "Reservation created successfully.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) 
                return NotFound();

            Reservation reservation = await reservationContext.ReadAsync(id.Value);
            if (reservation == null) return NotFound();

            if (reservation.IsConfirmed)
            {
                TempData["Error"] = "Cannot delete a confirmed reservation.";
                return RedirectToAction(nameof(Index));
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]

        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Reservation reservation = await reservationContext.ReadAsync(id);
            if (reservation == null) return NotFound();

            if (reservation.IsConfirmed)
            {
                TempData["Error"] = "Cannot delete a confirmed reservation.";
                return RedirectToAction(nameof(Index));
            }

            await reservationContext.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task SendConfirmationEmailAsync(Reservation reservation, Flight flight)
        {
            await Task.CompletedTask;
        }
    }
}
