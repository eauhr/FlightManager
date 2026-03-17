using FlightManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data
{
    public class FlightContext : IDb<Flight, int>, IQueryDb<Flight, int>
    {
        private readonly MVCDbContext context;
        public FlightContext(MVCDbContext context)
        {
            this.context = context;
        }

        public async Task CreateAsync(Flight item)
        {
            context.Flights.Add(item);
            await context.SaveChangesAsync();
        }

        public async Task<Flight> ReadAsync(int key)
        {
            try
            {
                return await context.Flights
                    .Include(f => f.Reservations)
                        .ThenInclude(r => r.Passengers)
                    .FirstOrDefaultAsync(f => f.Id == key);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<Flight>> ReadAllAsync()
        {
            try
            {
                return await context.Flights.ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task UpdateAsync(Flight item)
        {
            try
            {
                context.Flights.Update(item);
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
                Flight flight = await ReadAsync(key);
                if (flight != null)
                {
                    context.Flights.Remove(flight);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<Flight>> GetFilteredAsync(string? from, string? to, int page, int pageSize)
        {
            try
            {
                IQueryable<Flight> query = context.Flights.AsQueryable();

                if (!string.IsNullOrEmpty(from))
                    query = query.Where(f => f.DepartureCity.Contains(from));

                if (!string.IsNullOrEmpty(to))
                    query = query.Where(f => f.ArrivalCity.Contains(to));

                return await query
                    .OrderBy(f => f.DepartureTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            { 
                throw ex; 
            }
        }

        public async Task<int> GetCountAsync(string? from, string? to)
        {
            try
            {
                IQueryable<Flight> query = context.Flights.AsQueryable();

                if (!string.IsNullOrEmpty(from))
                    query = query.Where(f => f.DepartureCity.Contains(from));

                if (!string.IsNullOrEmpty(to))
                    query = query.Where(f => f.ArrivalCity.Contains(to));

                return await query.CountAsync();
            }
            catch (Exception ex) { throw ex; }
        }


        public bool Exists(int key)
        {
            return context.Flights.Any(f => f.Id == key);
        }
    }
}
