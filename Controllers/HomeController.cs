using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MesaListo.Data;
using MesaListo.Models;

namespace MesaListo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var restaurantes = await _context.Restaurantes.ToListAsync();
            return View(restaurantes);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}