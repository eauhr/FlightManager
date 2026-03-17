using FlightManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data
{
    public class IdentityContext
    {
        private readonly UserManager<User> userManager;
        private readonly MVCDbContext context;

        public IdentityContext(MVCDbContext context, UserManager<User> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        #region Seeding

        public async Task SeedDataAsync(string adminPass, string adminEmail)
        {
            int userRoles = await context.UserRoles.CountAsync();

            if (userRoles == 0)
            {
                await ConfigureAdminAccountAsync(adminPass, adminEmail);
            }
        }

        public async Task ConfigureAdminAccountAsync(string password, string email)
        {
            User adminUser = await context.Users.FirstOrDefaultAsync();

            if (adminUser != null)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                await userManager.AddPasswordAsync(adminUser, password);
                await userManager.SetEmailAsync(adminUser, email);
            }
        }

        #endregion

        #region CRUD

        public async Task CreateUserAsync(string username, string password, string email,
            string firstName, string lastName, string egn, string address, string phoneNumber, string role)
        {
            try
            {
                User user = new User
                {
                    UserName = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EGN = egn,
                    Address = address,
                    PhoneNumber = phoneNumber
                };

                IdentityResult result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    throw new ArgumentException(result.Errors.First().Description);
                }

                await userManager.AddToRoleAsync(user, role);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<User> ReadUserAsync(string id)
        {
            try
            {
                return await userManager.FindByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<User>> ReadAllUsersAsync()
        {
            try
            {
                return await context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<User>> GetFilteredAsync(string? email, string? username,
            string? firstName, string? lastName, int page, int pageSize)
        {
            try
            {
                var query = context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(email))
                    query = query.Where(u => u.Email.Contains(email));

                if (!string.IsNullOrEmpty(username))
                    query = query.Where(u => u.UserName.Contains(username));

                if (!string.IsNullOrEmpty(firstName))
                    query = query.Where(u => u.FirstName.Contains(firstName));

                if (!string.IsNullOrEmpty(lastName))
                    query = query.Where(u => u.LastName.Contains(lastName));

                return await query
                    .OrderBy(u => u.UserName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<int> GetCountAsync(string? email, string? username,
            string? firstName, string? lastName)
        {
            try
            {
                var query = context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(email))
                    query = query.Where(u => u.Email.Contains(email));

                if (!string.IsNullOrEmpty(username))
                    query = query.Where(u => u.UserName.Contains(username));

                if (!string.IsNullOrEmpty(firstName))
                    query = query.Where(u => u.FirstName.Contains(firstName));

                if (!string.IsNullOrEmpty(lastName))
                    query = query.Where(u => u.LastName.Contains(lastName));

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task UpdateUserAsync(string id, string username, string email,
            string firstName, string lastName, string egn, string address, string phoneNumber)
        {
            try
            {
                User user = await userManager.FindByIdAsync(id);

                if (user == null)
                    throw new InvalidOperationException("User not found!");

                user.UserName = username;
                user.Email = email;
                user.FirstName = firstName;
                user.LastName = lastName;
                user.EGN = egn;
                user.Address = address;
                user.PhoneNumber = phoneNumber;

                IdentityResult result = await userManager.UpdateAsync(user);

                if (!result.Succeeded)
                    throw new ArgumentException(result.Errors.First().Description);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task DeleteUserAsync(string id)
        {
            try
            {
                User user = await userManager.FindByIdAsync(id);

                if (user == null)
                    throw new InvalidOperationException("User not found!");

                IdentityResult result = await userManager.DeleteAsync(user);

                if (!result.Succeeded)
                    throw new ArgumentException(result.Errors.First().Description);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<User> FindUserByNameAsync(string username)
        {
            try
            {
                return await userManager.FindByNameAsync(username);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}

