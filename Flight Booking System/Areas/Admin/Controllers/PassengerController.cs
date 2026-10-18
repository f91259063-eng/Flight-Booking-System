using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PassengerController : Controller
    {
        private readonly IRepository<Passenger> _repositoryPassenger;

        public PassengerController(IRepository<Passenger> repository)
        {
            _repositoryPassenger = repository;
        }

        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var passengers = await _repositoryPassenger.GetAllAsync();

            return View(passengers);
        }

       
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Passenger passenger)
        {
            if (!ModelState.IsValid)
                return View(passenger);

            await _repositoryPassenger.InsertAsync(passenger);

            var result = await _repositoryPassenger.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "تعذر حفظ بيانات الراكب."
                );

                return View(passenger);
            }

            return RedirectToAction(nameof(Index));
        }

     
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var passenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (passenger == null)
                return NotFound();

            return View(passenger);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Passenger passenger)
        {
            if (id != passenger.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(passenger);

            var existingPassenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (existingPassenger == null)
                return NotFound();

            existingPassenger.FirstName = passenger.FirstName;
            existingPassenger.MiddleName = passenger.MiddleName;
            existingPassenger.LastName = passenger.LastName;
            existingPassenger.PassportNumber = passenger.PassportNumber;
            existingPassenger.NationalID = passenger.NationalID;
            existingPassenger.Phone = passenger.Phone;
            existingPassenger.Email = passenger.Email;
            existingPassenger.DateOfBirth = passenger.DateOfBirth;

            var result = await _repositoryPassenger.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "تعذر تحديث بيانات الراكب."
                );

                return View(passenger);
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var passenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (passenger == null)
                return NotFound();

            return View(passenger);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var passenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (passenger == null)
                return NotFound();

            _repositoryPassenger.Delete(passenger);

            var result = await _repositoryPassenger.CommitAsync();

            if (result < 0)
            {
                return BadRequest(
                    "تعذر حذف الراكب، قد تكون بياناته مرتبطة بسجلات أخرى."
                );
            }

            return RedirectToAction(nameof(Index));
        }
    }
}