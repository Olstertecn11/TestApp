using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TestAplication.Models;
using System.Text.Json;

namespace TestAplication.Controllers;

public class CargaSucursalController : Controller
{
  private readonly ILogger<CargaSucursalController> _logger;

  public CargaSucursalController(ILogger<CargaSucursalController> logger)
  {
    _logger = logger;
  }

  public IActionResult Index()
  {
    return View();
  }


  [HttpPost]
  public async Task<IActionResult> Upload(IFormFile file)
  {
    if (file == null || file.Length == 0)
    {
      _logger.LogWarning("No se subió ningún archivo.");
      return BadRequest("No se subió ningún archivo.");
    }

    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();

    _logger.LogInformation("Contenido del JSON:\n{json}", content);
    Console.WriteLine("Contenido del JSON:");
    Console.WriteLine(content);

    try
    {
      var json = JsonDocument.Parse(content);
      _logger.LogInformation("Archivo JSON válido con {count} elementos en raíz.", json.RootElement.EnumerateObject().Count());
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Error al parsear el JSON.");
      return BadRequest("El archivo no tiene formato JSON válido.");
    }

    return RedirectToAction("Index");
  }



  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
