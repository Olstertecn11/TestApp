using TestAplication.Models;
using TestAplication.Models.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TestAplication.Models;
using System.Text.Json;
using TestAplication.Data;
using Microsoft.EntityFrameworkCore;


namespace TestAplication.Controllers;

public class CargaSucursalController : Controller
{
  private readonly ILogger<CargaSucursalController> _logger;
  private readonly ApplicationDbContext _context;

  public CargaSucursalController(ILogger<CargaSucursalController> logger, ApplicationDbContext context)
  {
    _logger = logger;
    _context = context;
  }

  public IActionResult New()
  {
    return View();
  }

  public IActionResult Index()
  {
    return View();
  }


  [HttpPost]
  public async Task<IActionResult> Upload(IFormFile file)
  {
    if (file == null || file.Length == 0)
      return BadRequest("No se subió ningún archivo.");

    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();

    using var json = JsonDocument.Parse(content);
    var empresaElement = json.RootElement.GetProperty("empresa");

    var nombreEmpresa = empresaElement.GetProperty("nombre").GetString()!;
    var paisEmpresa = empresaElement.GetProperty("pais").GetString()!;

    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
      // 🔹 Crear el registro del historial de carga
      var historial = new HistorialCarga
      {
        NombreArchivo = file.FileName,
        FechaCarga = DateTime.Now,
        Estado = "Completado",
        IpCarga = HttpContext.Connection.RemoteIpAddress?.ToString()
      };

      _context.HistorialCargas.Add(historial);
      await _context.SaveChangesAsync();

      // 🔹 EMPRESA: verifica si ya existe
      var empresa = await _context.Empresas
        .FirstOrDefaultAsync(e => e.Nombre == nombreEmpresa && e.Pais == paisEmpresa);

      if (empresa == null)
      {
        empresa = new Empresa
        {
          Nombre = nombreEmpresa,
          Pais = paisEmpresa,
          IdHistorialCargaFk = historial.IdHistorialCarga
        };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
      }
      else
      {
        empresa.IdHistorialCargaFk = historial.IdHistorialCarga;
        _context.Update(empresa);
        await _context.SaveChangesAsync();
      }

      int totalSucursales = 0;
      int totalColaboradores = 0;

      // 🔹 SUCURSALES
      var sucursales = empresaElement.GetProperty("sucursales").EnumerateArray();

      foreach (var s in sucursales)
      {
        var nombreSucursal = s.GetProperty("nombre").GetString()!;
        var direccion = s.GetProperty("direccion").GetString()!;

        var sucursal = await _context.Sucursales
          .FirstOrDefaultAsync(x =>
              x.Nombre == nombreSucursal &&
              x.Direccion == direccion &&
              x.IdEmpresaFk == empresa.IdEmpresa);

        if (sucursal == null)
        {
          sucursal = new Sucursal
          {
            IdEmpresaFk = empresa.IdEmpresa,
            Nombre = nombreSucursal,
            Direccion = direccion
          };
          _context.Sucursales.Add(sucursal);
          await _context.SaveChangesAsync();
          totalSucursales++;
        }

        // 🔹 COLABORADORES
        var colaboradores = s.GetProperty("colaboradores").EnumerateArray();

        foreach (var c in colaboradores)
        {
          var nombreColaborador = c.GetProperty("nombre").GetString()!;
          var cui = c.GetProperty("CUI").GetString();

          var existeColaborador = await _context.Colaboradores
            .AnyAsync(x =>
                x.Nombre == nombreColaborador &&
                x.CUI == cui &&
                x.IdSucursalFk == sucursal.IdSucursal);

          if (!existeColaborador)
          {
            var colaborador = new Colaborador
            {
              IdSucursalFk = sucursal.IdSucursal,
              Nombre = nombreColaborador,
              CUI = cui
            };
            _context.Colaboradores.Add(colaborador);
            totalColaboradores++;
          }
        }
      }

      await _context.SaveChangesAsync();

      // 🔹 Actualiza totales en el historial
      historial.TotalEmpresas = 1;
      historial.TotalSucursales = totalSucursales;
      historial.TotalColaboradores = totalColaboradores;

      _context.HistorialCargas.Update(historial);
      await _context.SaveChangesAsync();

      await transaction.CommitAsync();

      TempData["Success"] = $"Carga exitosa: {file.FileName}";
      return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();

      _logger.LogError(ex, "Error al procesar el archivo JSON.");

      // Registrar en historial si ya fue creado
      var historialError = new HistorialCarga
      {
        NombreArchivo = file.FileName,
        FechaCarga = DateTime.Now,
        Estado = "Error",
        MensajeError = ex.Message,
        IpCarga = HttpContext.Connection.RemoteIpAddress?.ToString()
      };
      _context.HistorialCargas.Add(historialError);
      await _context.SaveChangesAsync();

      return StatusCode(500, "Ocurrió un error al procesar el archivo.");
    }
  }


  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
