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
    public class PassengersController : Controller
    {
        private readonly PassengerContext passengerContext;
        private readonly ReservationContext reservationContext;

        public PassengersController(PassengerContext passengerContext, ReservationContext reservationContext)
        {
            this.passengerContext = passengerContext;
            this.reservationContext = reservationContext;
        }

        // GET: Passengers
        public async Task<IActionResult> Index()
        {
            return View(await passengerContext.ReadAllAsync());
        }

        // GET: Passengers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            Passenger passenger = await passengerContext.ReadAsync(id.Value);
            if (passenger == null) return NotFound();

            return View(passenger);
        }

        public async Task<IActionResult> Create()
        {
            IEnumerable<Reservation> reservations = await reservationContext.ReadAllAsync();
            ViewData["ReservationId"] = new SelectList(reservations, "Id", "ContactEmail");
            return View();
        }

        // POST: Passengers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,MiddleName,LastName,EGN,PhoneNumber,Nationality,Type,ReservationId")] Passenger passenger)
        {
            if (!ModelState.IsValid)
            {
                IEnumerable<Reservation> reservations = await reservationContext.ReadAllAsync();
                ViewData["ReservationId"] = new SelectList(reservations, "Id", "ContactEmail", passenger.ReservationId);
                return View(passenger);
            }

            try
            {
                await passengerContext.CreateAsync(passenger);
                TempData["Success"] = "Passenger added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                IEnumerable<Reservation> reservations = await reservationContext.ReadAllAsync();
                ViewData["ReservationId"] = new SelectList(reservations, "Id", "ContactEmail", passenger.ReservationId);
                return View(passenger);
            }
        }

        // GET: Passengers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Passenger passenger = await passengerContext.ReadAsync(id.Value);
            if (passenger == null) return NotFound();

            IEnumerable<Reservation> reservations = await reservationContext.ReadAllAsync();
            ViewData["ReservationId"] = new SelectList(reservations, "Id", "ContactEmail", passenger.ReservationId);
            return View(passenger);
        }

        // POST: Passengers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,MiddleName,LastName,EGN,PhoneNumber,Nationality,Type,ReservationId")] Passenger passenger)
        {
            if (id != passenger.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                IEnumerable<Reservation> reservations = await reservationContext.ReadAllAsync();
                ViewData["ReservationId"] = new SelectList(reservations, "Id", "ContactEmail", passenger.ReservationId);
                return View(passenger);
            }

            try
            {
                await passengerContext.UpdateAsync(passenger);
                TempData["Success"] = "Passenger updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!passengerContext.Exists(passenger.Id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                IEnumerable<Reservation> reservations = await reservationContext.ReadAllAsync();
                ViewData["ReservationId"] = new SelectList(reservations, "Id", "ContactEmail", passenger.ReservationId);
                return View(passenger);
            }
        }

        // GET: Passengers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Passenger passenger = await passengerContext.ReadAsync(id.Value);
            if (passenger == null) return NotFound();

            return View(passenger);
        }

        // POST: Passengers/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await passengerContext.DeleteAsync(id);
                TempData["Success"] = "Passenger deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private bool PassengerExists(int id)
        {
            return passengerContext.Exists(id);
        }
    }
}
