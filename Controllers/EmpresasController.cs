using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAplication.Data;
using TestAplication.Models.Data;

namespace TestAplication.Controllers
{
    public class EmpresasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpresasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Empresas
        public async Task<IActionResult> Index()
        {
            var empresas = await _context.Empresas
                .Include(e => e.HistorialCarga)
                .Where(e => e.EstaActivo)
                .ToListAsync();
            return View(empresas);
        }

        // GET: Empresas/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        // POST: Empresas/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Empresa empresa)
        {
            if (id != empresa.IdEmpresa)
                return BadRequest();

            if (!ModelState.IsValid)
                return View("Edit", empresa);

            try
            {
                _context.Update(empresa);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Empresa actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
                return View("Edit", empresa);
            }
        }

        // POST: Empresas/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
          var empresa = await _context.Empresas
            .Include(e => e.Sucursales)
            .ThenInclude(s => s.Colaboradores)
            .FirstOrDefaultAsync(e => e.IdEmpresa == id);

          if (empresa == null)
            return NotFound();

          try
          {
            // 🔹 Desactivar colaboradores
            foreach (var sucursal in empresa.Sucursales)
            {
              foreach (var colaborador in sucursal.Colaboradores)
              {
                colaborador.EstaActivo = false;
                _context.Update(colaborador);
              }

              // 🔹 Desactivar sucursal
              sucursal.EstaActivo = false;
              _context.Update(sucursal);
            }

            // 🔹 Desactivar empresa
            empresa.EstaActivo = false;
            _context.Update(empresa);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Empresa, sucursales y colaboradores desactivados correctamente.";
          }
          catch (DbUpdateException ex)
          {
            TempData["Error"] = $"Error al desactivar: {ex.Message}";
          }

          return RedirectToAction(nameof(Index));
        }
    }
}
