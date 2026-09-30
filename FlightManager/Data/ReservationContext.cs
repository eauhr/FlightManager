using FlightManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FlightManager.Data
{
    public class ReservationContext : IDb<Reservation, int>, IQueryDb<Reservation, int>
    {
        private readonly MVCDbContext context;

        public ReservationContext(MVCDbContext context)
        {
            this.context = context;
        }

        // Called only from TryCreateAsync now — kept private
        private async Task<int> CountSeatsAsync(int flightId, TypeTicket type)
        {
            return await context.Passengers
                .Where(p => p.Reservation.FlightId == flightId
                         && p.Reservation.IsConfirmed
                         && p.Type == type)
                .CountAsync();
        }

        // Atomic check-and-create. Returns null on success, error message on failure.
        public async Task<string?> TryCreateAsync(
            Reservation reservation,
            int economyCapacity,
            int businessCapacity)
        {
            using var transaction = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                int economyTaken = await CountSeatsAsync(reservation.FlightId, TypeTicket.Economy);
                int businessTaken = await CountSeatsAsync(reservation.FlightId, TypeTicket.Business);

                int economyRequested = reservation.Passengers.Count(p => p.Type == TypeTicket.Economy);
                int businessRequested = reservation.Passengers.Count(p => p.Type == TypeTicket.Business);

                int economyAvailable = economyCapacity - economyTaken;
                int businessAvailable = businessCapacity - businessTaken;

                if (economyRequested > economyAvailable)
                    return $"Not enough economy seats. Available: {economyAvailable}";

                if (businessRequested > businessAvailable)
                    return $"Not enough business seats. Available: {businessAvailable}";

                context.Reservations.Add(reservation);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return null;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CreateAsync(Reservation item)
        {
            context.Reservations.Add(item);
            await context.SaveChangesAsync();
        }

        public async Task<Reservation> ReadAsync(int key)
        {
            return await context.Reservations
                .Include(r => r.Passengers)
                .Include(r => r.Flight)
                .FirstOrDefaultAsync(r => r.Id == key);
        }

        public async Task<IEnumerable<Reservation>> ReadAllAsync()
        {
            return await context.Reservations
                .Include(r => r.Passengers)
                .Include(r => r.Flight)
                .ToListAsync();
        }

        public async Task UpdateAsync(Reservation item)
        {
            context.Reservations.Update(item);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int key)
        {
            Reservation reservation = await context.Reservations.FindAsync(key);
            if (reservation != null)
            {
                context.Reservations.Remove(reservation);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Reservation>> GetFilteredAsync(string? email, int page, int pageSize)
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

        public async Task<int> GetCountAsync(string? email)
        {
            IQueryable<Reservation> query = context.Reservations.AsQueryable();

            if (!string.IsNullOrEmpty(email))
                query = query.Where(r => r.ContactEmail.Contains(email));

            return await query.CountAsync();
        }

        public bool Exists(int key)
        {
            return context.Reservations.Any(r => r.Id == key);
        }
    }
}