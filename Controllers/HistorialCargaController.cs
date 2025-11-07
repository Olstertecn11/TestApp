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
            .Where(e => e.IdHistorialCargaFk == id && e.EstaActivo)
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
              where s.IdHistorialCargaFk == id && s.EstaActivo && e.EstaActivo
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
              where c.IdHistorialCargaFk == id && c.EstaActivo && s.EstaActivo && e.EstaActivo
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


    }



}
