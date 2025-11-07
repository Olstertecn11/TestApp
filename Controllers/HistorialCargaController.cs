using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAplication.Data;
using TestAplication.Models.Data;

namespace TestAplication.Controllers
{
    public class HistorialCargaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistorialCargaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HistorialCarga
        public async Task<IActionResult> Index()
        {
            var historial = await _context.HistorialCargas
                .Include(h => h.Empresas)
                .OrderByDescending(h => h.FechaCarga)
                .ToListAsync();

            return View(historial);
        }
    }
}
