
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CityController : Controller
    {
        private readonly IRepository<city> _repositorycity;


        public CityController(IRepository<city> repository)
        {
            _repositorycity = repository;
        }

      
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cities = await _repositorycity.GetAllAsync();

            return View(cities);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        public async Task<IActionResult> Create(city city)
        {
            if (ModelState.IsValid)
            {
                await _repositorycity.InsertAsync(city);
                await _repositorycity.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(city);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var city = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

    
        [HttpPost]
       
        public async Task<IActionResult> Edit(int id, city city)
        {
            if (id != city.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingCity = await _repositorycity
                    .GetOneAsync(x => x.Id == id);

                if (existingCity == null)
                {
                    return NotFound();
                }

             
                existingCity.Name = city.Name;

                await _repositorycity.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(city);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Admin/City/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var city = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (city == null)
            {
                return NotFound();
            }

             _repositorycity.Delete(city);
            await _repositorycity.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
