using System.ComponentModel.DataAnnotations;

namespace TestAplication.Models.Data
{
    public class Empresa
    {
        [Key]
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = null!;
        public string Pais { get; set; } = null!;

        // Relación con Sucursales (1 a muchos)
        public ICollection<Sucursal>? Sucursales { get; set; }

        public int? IdHistorialCargaFk { get; set; }
        public HistorialCarga? HistorialCarga { get; set; }
    }
}
