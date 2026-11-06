
using Flight_Booking_System.Data;
using Flight_Booking_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Services
{
    public class SeatService : ISeatService
    {
        private readonly ApplicationDbContext _context;

        public SeatService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Generate seats for a newly created AircraftClass.
        public async Task GenerateSeatsAsync(AircraftClass aircraftClass)
        {
            if (aircraftClass == null)
                throw new ArgumentNullException(nameof(aircraftClass));

            if (aircraftClass.NumberOfSeats <= 0)
                return;

            var existingSeats = await _context.Set<Seat>()
                .Where(s => s.AircraftClassId == aircraftClass.ID)
                .ToListAsync();

            if (existingSeats.Count >= aircraftClass.NumberOfSeats)
                return;

            var existingNumbers = existingSeats
                .Select(s => s.SeatNumber)
                .ToHashSet();

            int maxNumber = existingSeats
                .Select(s => int.TryParse(s.SeatNumber, out var number)
                    ? number
                    : 0)
                .DefaultIfEmpty(0)
                .Max();

            int createdCount = existingSeats.Count;

            while (createdCount < aircraftClass.NumberOfSeats)
            {
                maxNumber++;

                string seatNumber = maxNumber.ToString("D3");

                if (!existingNumbers.Add(seatNumber))
                    continue;

                _context.Set<Seat>().Add(new Seat
                {
                    AircraftClassId = aircraftClass.ID,
                    SeatNumber = seatNumber
                });

                createdCount++;
            }

            await _context.SaveChangesAsync();
        }

        // Increase or decrease the number of seats safely.
        public async Task<(bool Success, string? Error)> ResizeSeatsAsync(
            int aircraftClassId,
            int targetSeatCount)
        {
            if (targetSeatCount <= 0)
            {
                return (false, "The number of seats must be greater than zero.");
            }

            var seats = await _context.Set<Seat>()
                .Where(s => s.AircraftClassId == aircraftClassId)
                .ToListAsync();

            int currentCount = seats.Count;

            // Nothing to change.
            if (currentCount == targetSeatCount)
                return (true, null);

            // Increase seats.
            if (targetSeatCount > currentCount)
            {
                var existingNumbers = seats
                    .Select(s => s.SeatNumber)
                    .ToHashSet();

                int maxNumber = seats
                    .Select(s => int.TryParse(s.SeatNumber, out var number)
                        ? number
                        : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                int createdCount = currentCount;

                while (createdCount < targetSeatCount)
                {
                    maxNumber++;

                    string seatNumber = maxNumber.ToString("D3");

                    if (!existingNumbers.Add(seatNumber))
                        continue;

                    _context.Set<Seat>().Add(new Seat
                    {
                        AircraftClassId = aircraftClassId,
                        SeatNumber = seatNumber
                    });

                    createdCount++;
                }

                return (true, null);
            }

            // Decrease seats.
            int seatsToRemove = currentCount - targetSeatCount;

            var seatIds = seats.Select(s => s.Id).ToList();

            var bookedSeatIds = await _context.Set<Ticket>()
                .Where(t => seatIds.Contains(t.SeatId))
                .Select(t => t.SeatId)
                .Distinct()
                .ToListAsync();

            var bookedIds = bookedSeatIds.ToHashSet();

            var removableSeats = seats
                .Where(s => !bookedIds.Contains(s.Id))
                .OrderByDescending(s =>
                    int.TryParse(s.SeatNumber, out var number)
                        ? number
                        : 0)
                .Take(seatsToRemove)
                .ToList();

            if (removableSeats.Count < seatsToRemove)
            {
                return (
                    false,
                    $"Cannot reduce the seat count to {targetSeatCount}. " +
                    $"Some seats are linked to existing tickets. " +
                    $"Only {seats.Count - bookedSeatIds.Count} seats can be safely removed."
                );
            }

            _context.Set<Seat>().RemoveRange(removableSeats);

            return (true, null);
        }

        // Remove all seats only when none is linked to a ticket.
        public async Task<(bool Success, string? Error)> RemoveSeatsAsync(
            int aircraftClassId)
        {
            var seats = await _context.Set<Seat>()
                .Where(s => s.AircraftClassId == aircraftClassId)
                .ToListAsync();

            if (!seats.Any())
                return (true, null);

            var seatIds = seats.Select(s => s.Id).ToList();

            bool hasTickets = await _context.Set<Ticket>()
                .AnyAsync(t => seatIds.Contains(t.SeatId));

            if (hasTickets)
            {
                return (
                    false,
                    "This aircraft class cannot be deleted because some of its seats are linked to existing tickets."
                );
            }

            _context.Set<Seat>().RemoveRange(seats);

            return (true, null);
        }
    }
}
