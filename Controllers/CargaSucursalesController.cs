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
    var nombreEmpresa = empresaElement.GetProperty("nombre").GetString()?.Trim() ?? "";
    var paisEmpresa = empresaElement.GetProperty("pais").GetString()?.Trim() ?? "";

    using var transaction = await _context.Database.BeginTransactionAsync();
    bool inserted = false;

    try
    {
      // 🧾 Crear registro en HistorialCarga
      var historial = new HistorialCarga
      {
        NombreArchivo = file.FileName,
        FechaCarga = DateTime.Now,
        Estado = "Completado",
        IpCarga = HttpContext.Connection.RemoteIpAddress?.ToString()
      };

      _context.HistorialCargas.Add(historial);
      await _context.SaveChangesAsync();

      // 📂 Ruta del archivo log
      var logPath = Path.Combine(Directory.GetCurrentDirectory(), $"LogCargaInformacion_{historial.IdHistorialCarga}.txt");
      await System.IO.File.WriteAllTextAsync(logPath, $"🕒 Log de carga {DateTime.Now}\nArchivo: {file.FileName}\n\n");

      int totalSucursales = 0;
      int totalColaboradores = 0;

      // 🏢 Buscar empresa activa existente
      var empresa = await _context.Empresas
        .FirstOrDefaultAsync(e =>
            e.Nombre.ToLower() == nombreEmpresa.ToLower() &&
            e.Pais.ToLower() == paisEmpresa.ToLower() &&
            e.EstaActivo);

      if (empresa == null)
      {
        empresa = new Empresa
        {
          Nombre = nombreEmpresa,
          Pais = paisEmpresa,
          EstaActivo = true,
          IdHistorialCargaFk = historial.IdHistorialCarga
        };
        _context.Empresas.Add(empresa);
        inserted = true;
        await _context.SaveChangesAsync();
        await System.IO.File.AppendAllTextAsync(logPath, $"✅ Empresa creada: {empresa.Nombre} ({empresa.Pais})\n");
      }
      else
      {
        await System.IO.File.AppendAllTextAsync(
            logPath,
            $"⚠️ Empresa ya existente: {nombreEmpresa} ({paisEmpresa}){Environment.NewLine}"
            );
      }

      // 🏬 Procesar sucursales
      var sucursales = empresaElement.GetProperty("sucursales").EnumerateArray();

      foreach (var s in sucursales)
      {
        var nombreSucursal = s.GetProperty("nombre").GetString()?.Trim() ?? "";
        var direccion = s.GetProperty("direccion").GetString()?.Trim() ?? "";

        var sucursal = await _context.Sucursales
          .FirstOrDefaultAsync(x =>
              x.Nombre.ToLower() == nombreSucursal.ToLower() &&
              x.Direccion.ToLower() == direccion.ToLower() &&
              x.IdEmpresaFk == empresa.IdEmpresa &&
              x.EstaActivo);

        if (sucursal == null)
        {
          sucursal = new Sucursal
          {
            IdEmpresaFk = empresa.IdEmpresa,
            Nombre = nombreSucursal,
            Direccion = direccion,
            EstaActivo = true,
            IdHistorialCargaFk = historial.IdHistorialCarga
          };
          _context.Sucursales.Add(sucursal);
          await _context.SaveChangesAsync();
          totalSucursales++;
          await System.IO.File.AppendAllTextAsync(logPath, $"✅ Sucursal creada: {nombreSucursal}\n");
        }
        else
        {
          await System.IO.File.AppendAllTextAsync(logPath, $"⚠️ Sucursal ya existente: {nombreSucursal}\n");
        }

        // 👥 Procesar colaboradores
        var colaboradores = s.GetProperty("colaboradores").EnumerateArray();

        foreach (var c in colaboradores)
        {
          var nombreColaborador = c.GetProperty("nombre").GetString()?.Trim() ?? "";
          var cui = c.TryGetProperty("CUI", out var cuiProp)
            ? cuiProp.GetString()?.Trim() ?? ""
            : "";

          var existeColaborador = await _context.Colaboradores
            .AnyAsync(x =>
                x.IdSucursalFk == sucursal.IdSucursal &&
                x.EstaActivo &&
                x.Nombre.ToLower() == nombreColaborador.ToLower() &&
                ((x.CUI ?? "") == (cui ?? "")));

          if (!existeColaborador)
          {
            var colaborador = new Colaborador
            {
              IdSucursalFk = sucursal.IdSucursal,
              Nombre = nombreColaborador,
              CUI = cui,
              EstaActivo = true,
              IdHistorialCargaFk = historial.IdHistorialCarga
            };
            _context.Colaboradores.Add(colaborador);
            totalColaboradores++;
            await System.IO.File.AppendAllTextAsync(logPath, $"✅ Colaborador agregado: {nombreColaborador} (CUI: {cui})\n");
          }
          else
          {
            await System.IO.File.AppendAllTextAsync(logPath, $"⚠️ Colaborador ya existente: {nombreColaborador} (CUI: {cui})\n");
          }
        }
      }

      await _context.SaveChangesAsync();

      // 📊 Actualizar historial con totales
      historial.TotalEmpresas = inserted ? 1 : 0;
      historial.TotalSucursales = totalSucursales;
      historial.TotalColaboradores = totalColaboradores;
      _context.HistorialCargas.Update(historial);

      await _context.SaveChangesAsync();
      await transaction.CommitAsync();

      await System.IO.File.AppendAllTextAsync(logPath, "\n✅ Carga completada correctamente.\n");

      TempData["Success"] = $"Carga exitosa: {file.FileName}. Detalles en LogCargaInformacion_{historial.IdHistorialCarga}.txt";
      return RedirectToAction("Index", "HistorialCarga");
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "Error al procesar el archivo JSON.");

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

      var logPathError = Path.Combine(Directory.GetCurrentDirectory(), $"LogCargaInformacion_Error_{DateTime.Now:yyyyMMddHHmmss}.txt");
      await System.IO.File.WriteAllTextAsync(logPathError, $"❌ Error al procesar la carga:\n{ex.Message}");

      return StatusCode(500, "Ocurrió un error al procesar el archivo.");
    }
  }


  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
