using FlightManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data
{
    public class ReservationContext : IDb<Reservation, int>, IQueryDb<Reservation, int>
    {
        private readonly MVCDbContext context;
        public ReservationContext(MVCDbContext context)
        {
            this.context = context;
        }
        public async Task CreateAsync(Reservation item)
        {
            context.Reservations.Add(item);
            await context.SaveChangesAsync();
        }
        public async Task<Reservation> ReadAsync(int key)
        {
            try
            {
                return await context.Reservations
                      .Include(r => r.Passengers)
                      .Include(r => r.Flight)
                      .FirstOrDefaultAsync(r => r.Id == key);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IEnumerable<Reservation>> ReadAllAsync()
        {
            try
            {
                return await context.Reservations
                  .Include(r => r.Passengers)
                  .Include(r => r.Flight)
                  .ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task UpdateAsync(Reservation item)
        {
            try
            {
                context.Reservations.Update(item);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task DeleteAsync(int key)
        {
            try
            {
                Reservation reservation = await context.Reservations.FindAsync(key);
                if (reservation != null)
                {
                    context.Reservations.Remove(reservation);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<Reservation>> GetFilteredAsync(string? email, int page, int pageSize)
        {
            try
            {
                IQueryable<Reservation> query = context.Reservations.AsQueryable();

                if (!string.IsNullOrEmpty(email))
                    query = query.Where(r => r.ContactEmail.Contains(email));

                return await query
                    .OrderBy(r => r.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex) 
            {
                throw ex;
            }
        }

        public async Task<int> GetCountAsync(string? email)
        {
            try
            {
                IQueryable<Reservation> query = context.Reservations.AsQueryable();

                if (!string.IsNullOrEmpty(email))
                    query = query.Where(r => r.ContactEmail.Contains(email));

                return await query.CountAsync();
            }
            catch (Exception ex) { throw ex; }
        }


        public bool Exists(int key)
        {
            return context.Reservations.Any(r => r.Id == key);
        }
    }
}
