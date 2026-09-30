using FlightManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data
{
    public class PassengerContext : IDb<Passenger, int>, IQueryDb<Passenger, int>
    {
        private readonly MVCDbContext context;
        public PassengerContext(MVCDbContext context)
        {
            this.context = context;
        }
        public async Task CreateAsync(Passenger item)
        {
            context.Passengers.Add(item);
            await context.SaveChangesAsync();
        }
        public async Task<Passenger> ReadAsync(int key)
        {
            return await context.Passengers
                      .Include(p => p.Reservation)
                      .ThenInclude(r => r.Flight)
                      .FirstOrDefaultAsync(p => p.Id == key);
        }
        public async Task<IEnumerable<Passenger>> ReadAllAsync()
        {
            return await context.Passengers
                  .Include(p => p.Reservation)
                  .ThenInclude(r => r.Flight)
                  .ToListAsync();
        }
        public async Task UpdateAsync(Passenger item)
        {
                context.Passengers.Update(item);
                await context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int key)
         {
            Passenger passenger = await context.Passengers.FindAsync(key);
                 if (passenger != null)
                 {
                     context.Passengers.Remove(passenger);
                     await context.SaveChangesAsync();
                 }
        }

        public async Task<IEnumerable<Passenger>> GetPassengersByEmailAsync(int flightId)
        {
                return await context.Passengers
                    .Include(p => p.Reservation)
                    .Where(p => p.Reservation.FlightId == flightId)
                    .ToListAsync();
        }

        public bool Exists(int id)
        {
            return context.Passengers.Any(p => p.Id == id);
        }


    }
}
