using System.ComponentModel.DataAnnotations;

namespace TestAplication.Models.Data
{
    public class Colaborador
    {
        [Key]
        public int IdColaborador { get; set; }
        public int IdSucursalFk { get; set; }
        public string Nombre { get; set; } = null!;
        public string? CUI { get; set; }
        public bool EstaActivo { get; set; } = true;

        // Relación con Sucursal (N a 1)
        public Sucursal? Sucursal { get; set; }
    }
}
