using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAplication.Data;
using TestAplication.Models.Data;

namespace TestAplication.Controllers
{
    public class HistorialCargaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HistorialCargaController> _logger;

        public HistorialCargaController(ApplicationDbContext context, ILogger<HistorialCargaController> logger)
        {
            _context = context;
            _logger = logger;
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


        public async Task<IActionResult> Details(int id)
        {
          Console.WriteLine($"🔍 Buscando detalles para la carga con ID: {id}");
          var historial = await _context.HistorialCargas.FindAsync(id);
          if (historial == null)
            return NotFound();

          var logPath = Path.Combine(Directory.GetCurrentDirectory(), $"LogCargaInformacion_{id}.txt");
          string logContent = System.IO.File.Exists(logPath)
            ? await System.IO.File.ReadAllTextAsync(logPath)
            : "⚠️ No se encontró el archivo de log para esta carga.";

          var empresas = await _context.Empresas
            .Where(e => e.IdHistorialCargaFk == id)
            .Select(e => new
                {
                Tipo = "Empresa",
                Empresa = e.Nombre,
                Pais = e.Pais,
                Sucursal = "-",
                Direccion = "-",
                Colaborador = "-",
                CUI = "-"
                })
          .ToListAsync();

          var sucursales = await (from s in _context.Sucursales
              join e in _context.Empresas on s.IdEmpresaFk equals e.IdEmpresa
              where s.IdHistorialCargaFk == id 
              select new
              {
              Tipo = "Sucursal",
              Empresa = e.Nombre,
              Pais = e.Pais,
              Sucursal = s.Nombre,
              Direccion = s.Direccion,
              Colaborador = "-",
              CUI = "-"
              })
          .ToListAsync();

          var colaboradores = await (from c in _context.Colaboradores
              join s in _context.Sucursales on c.IdSucursalFk equals s.IdSucursal
              join e in _context.Empresas on s.IdEmpresaFk equals e.IdEmpresa
              where c.IdHistorialCargaFk == id 
              select new
              {
              Tipo = "Colaborador",
              Empresa = e.Nombre,
              Pais = e.Pais,
              Sucursal = s.Nombre,
              Direccion = s.Direccion,
              Colaborador = c.Nombre,
              CUI = c.CUI
              })
          .ToListAsync();

          var datos = empresas
            .Concat(sucursales)
            .Concat(colaboradores)
            .OrderBy(d => d.Empresa)
            .ThenBy(d => d.Sucursal)
            .ToList();

          ViewBag.LogContent = logContent;
          ViewBag.Historial = historial;
          return View(datos);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
          var historial = await _context.HistorialCargas.FindAsync(id);
          if (historial == null)
            return NotFound();

          using var transaction = await _context.Database.BeginTransactionAsync();

          try
          {
            var colaboradores = await _context.Colaboradores
              .Where(c =>
                  c.Sucursal.Empresa.IdHistorialCargaFk == id &&
                  c.EstaActivo)
              .ToListAsync();

            foreach (var col in colaboradores)
              col.EstaActivo = false;

            if (colaboradores.Any())
              _context.Colaboradores.UpdateRange(colaboradores);

            var sucursales = await _context.Sucursales
              .Where(s =>
                  s.Empresa.IdHistorialCargaFk == id &&
                  s.EstaActivo)
              .ToListAsync();

            foreach (var suc in sucursales)
              suc.EstaActivo = false;

            if (sucursales.Any())
              _context.Sucursales.UpdateRange(sucursales);

            var empresas = await _context.Empresas
              .Where(e =>
                  e.IdHistorialCargaFk == id &&
                  e.EstaActivo)
              .ToListAsync();

            foreach (var emp in empresas)
              emp.EstaActivo = false;

            if (empresas.Any())
              _context.Empresas.UpdateRange(empresas);

            historial.EstaActivo = false;
            historial.Estado = "Desactivado";

            _context.HistorialCargas.Update(historial);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Los registros del historial fueron desactivados correctamente.";
            return RedirectToAction(nameof(Index));
          }
          catch (Exception ex)
          {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al desactivar registros del historial.");
            TempData["Error"] = "Ocurrió un error al intentar desactivar los registros del historial.";
            return RedirectToAction(nameof(Index));
          }
        }


    }



}
