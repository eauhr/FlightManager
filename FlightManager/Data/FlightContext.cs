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
                return await context.Flights
                .Include(f => f.Reservations)
                .ThenInclude(r => r.Passengers)
                .FirstOrDefaultAsync(f => f.Id == key);
            
        }

        public async Task<IEnumerable<Flight>> ReadAllAsync()
        {
                return await context.Flights.ToListAsync();
        }

        public async Task UpdateAsync(Flight item)
        {
                context.Flights.Update(item);
                await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int key)
        {
                Flight flight = await ReadAsync(key);
                if (flight != null)
                {
                    context.Flights.Remove(flight);
                    await context.SaveChangesAsync();
                }
        }

        public async Task<IEnumerable<Flight>> GetFilteredAsync(string? from, string? to, int page, int pageSize)
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

        public async Task<int> GetCountAsync(string? from, string? to)
        {
                IQueryable<Flight> query = context.Flights.AsQueryable();

                if (!string.IsNullOrEmpty(from))
                    query = query.Where(f => f.DepartureCity.Contains(from));

                if (!string.IsNullOrEmpty(to))
                    query = query.Where(f => f.ArrivalCity.Contains(to));

                return await query.CountAsync();
        }


        public bool Exists(int key)
        {
            return context.Flights.Any(f => f.Id == key);
        }
    }
}
