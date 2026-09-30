using FlightManager.Data;
using FlightManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IdentityContext identityContext;

        public UsersController(IdentityContext identityContext)
        {
            this.identityContext = identityContext;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? email, string? username,
            string? firstName, string? lastName, int page = 1, int pageSize = 10)
        {
            IEnumerable<User> users = await identityContext.GetFilteredAsync(
                email, username, firstName, lastName, page, pageSize);
            int total = await identityContext.GetCountAsync(email, username, firstName, lastName);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Email = email;
            ViewBag.Username = username;
            ViewBag.FirstName = firstName;
            ViewBag.LastName = lastName;

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            User user = await identityContext.ReadUserAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create() => View();

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string email,
            string firstName, string lastName, string egn, string address, string phoneNumber)
        {
            if (!ModelState.IsValid) return View();

            try
            {
                await identityContext.CreateUserAsync(username, password, email,
                    firstName, lastName, egn, address, phoneNumber, "Employee");
                TempData["Success"] = $"User {username} created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            User user = await identityContext.ReadUserAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string username, string email,
            string firstName, string lastName, string egn, string address, string phoneNumber)
        {
            try
            {
                await identityContext.UpdateUserAsync(id, username, email,
                    firstName, lastName, egn, address, phoneNumber);
                TempData["Success"] = $"User {username} updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                User user = await identityContext.ReadUserAsync(id);
                return View(user);
            }
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            User user = await identityContext.ReadUserAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                User user = await identityContext.ReadUserAsync(id);
                await identityContext.DeleteUserAsync(id);
                TempData["Success"] = $"User {user.UserName} deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}