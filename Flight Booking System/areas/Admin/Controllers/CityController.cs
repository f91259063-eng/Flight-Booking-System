
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CityController : Controller
    {
        private readonly IRepository<city> _repositorycity;

        // Constructor Injection
        public CityController(IRepository<city> repository)
        {
            this._repositorycity = repository;
        }

        // GET: Admin/City/Index
        public async Task<IActionResult> Index()
        {
            var cities =  await _repositorycity.GetAllAsync();
            return View(cities);
        }

        public async Task<IActionResult> Create()
        {         
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(  city city)

        {
            if (city != null)
            {
               await _repositorycity.InsertAsync(city);
               await _repositorycity.CommitAsync();
            }





            return View( city);
        }

    }
}
