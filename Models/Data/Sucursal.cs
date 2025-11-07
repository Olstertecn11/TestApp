using System.ComponentModel.DataAnnotations;
namespace TestAplication.Models.Data
{
    public class Sucursal
    {
        [Key]
        public int IdSucursal { get; set; }
        public int IdEmpresaFk { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Direccion { get; set; }
        public bool EstaActivo { get; set; } = true;

        // Relación con Empresa (N a 1)
        public Empresa? Empresa { get; set; }

        // Relación con Colaboradores (1 a muchos)
        public ICollection<Colaborador>? Colaboradores { get; set; }

        public int? IdHistorialCargaFk { get; set; }
    }
}
