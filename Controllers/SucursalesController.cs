using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAplication.Data;
using TestAplication.Models.Data;

namespace TestAplication.Controllers
{
    public class SucursalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SucursalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sucursales = await _context.Sucursales
                .Include(s => s.Empresa)
                .ToListAsync();
            return View(sucursales);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var sucursal = await _context.Sucursales.FindAsync(id);
            if (sucursal == null)
                return NotFound();

            return View(sucursal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Sucursal sucursal)
        {
            if (id != sucursal.IdSucursal)
                return BadRequest();

            if (!ModelState.IsValid)
                return View("Edit", sucursal);

            try
            {
                _context.Update(sucursal);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Sucursal actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
                return View("Edit", sucursal);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sucursal = await _context.Sucursales.FindAsync(id);
            if (sucursal == null)
                return NotFound();

            _context.Sucursales.Remove(sucursal);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Sucursal eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
