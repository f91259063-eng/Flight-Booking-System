
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

        public async Task GenerateSeatsAsync(AircraftClass aircraftClass)
        {
            if (aircraftClass == null)
                throw new ArgumentNullException(nameof(aircraftClass));

            if (aircraftClass.NumberOfSeats <= 0)
                return;

            // المقاعد الموجودة بالفعل
            var existingSeats = await _context.Seats
                .Where(s => s.AircraftClassId == aircraftClass.ID)
                .ToListAsync();

            int existingCount = existingSeats.Count;

            // لو عدد المقاعد موجود بالفعل، لا ننشئ تكرار
            if (existingCount >= aircraftClass.NumberOfSeats)
                return;

            // إنشاء المقاعد الناقصة
            for (int i = existingCount + 1;
                 i <= aircraftClass.NumberOfSeats;
                 i++)
            {
                var seat = new Seat
                {
                    AircraftClassId = aircraftClass.ID,

                    // 001
                    // 002
                    // 003
                    // ...
                    SeatNumber = i.ToString("D3")
                };

                _context.Seats.Add(seat);
            }

            await _context.SaveChangesAsync();
        }
    }
}