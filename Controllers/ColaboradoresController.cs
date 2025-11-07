using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAplication.Data;
using TestAplication.Models.Data;

namespace TestAplication.Controllers
{
    public class ColaboradoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ColaboradoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var colaboradores = await _context.Colaboradores
                .Include(c => c.Sucursal)
                .Where(e => e.EstaActivo)
                .ToListAsync();
            return View(colaboradores);
        }

        public async Task<IActionResult> Edit(int id)
        {
          var colaborador = await _context.Colaboradores
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdColaborador == id);

          if (colaborador == null)
            return NotFound();

          return View(colaborador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [Bind("IdColaborador,Nombre,CUI")] Colaborador model)
        {
          if (id != model.IdColaborador)
            return BadRequest();

          if (!ModelState.IsValid)
          {
            TempData["Error"] = "Datos inválidos.";
            return View("Edit", model);
          }

          var colaborador = await _context.Colaboradores.FindAsync(id);
          if (colaborador == null)
            return NotFound();

          try
          {
            // 🔹 Solo actualizamos las propiedades necesarias
            colaborador.Nombre = model.Nombre;
            colaborador.CUI = model.CUI;

            _context.Colaboradores.Update(colaborador);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Colaborador actualizado correctamente.";
            return RedirectToAction(nameof(Index));
          }
          catch (DbUpdateException ex)
          {
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
            return View("Edit", model);
          }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
          var colaborador = await _context.Colaboradores.FindAsync(id);
          if (colaborador == null)
            return NotFound();

          try
          {
            colaborador.EstaActivo = false; // 🔹 Soft delete
            _context.Update(colaborador);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Colaborador desactivado correctamente.";
          }
          catch (DbUpdateException ex)
          {
            TempData["Error"] = $"Error al desactivar: {ex.Message}";
          }

          return RedirectToAction(nameof(Index));
        }
    }
}
