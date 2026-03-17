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
            try
            {
                return await context.Passengers
                      .Include(p => p.Reservation)
                      .ThenInclude(r => r.Flight)
                      .FirstOrDefaultAsync(p => p.Id == key);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IEnumerable<Passenger>> ReadAllAsync()
        {
            try
            {
                return await context.Passengers
                  .Include(p => p.Reservation)
                  .ThenInclude(r => r.Flight)
                  .ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task UpdateAsync(Passenger item)
        {
            try
            {
                context.Passengers.Update(item);
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
                 Passenger passenger = await context.Passengers.FindAsync(key);
                 if (passenger != null)
                 {
                     context.Passengers.Remove(passenger);
                     await context.SaveChangesAsync();
                 }
             }
             catch (Exception ex)
             {
                 throw ex;
             }
        }

        public async Task<IEnumerable<Passenger>> GetPassengersByEmailAsync(int flightId)
        {
            try
            {
                return await context.Passengers
                    .Include(p => p.Reservation)
                    .Where(p => p.Reservation.FlightId == flightId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Exists(int id)
        {
            return context.Passengers.Any(p => p.Id == id);
        }


    }
}
