using System.ComponentModel.DataAnnotations;

namespace TestAplication.Models.Data
{
    public class Pais
    {
        [Key]
        public int IdPais { get; set; }
        public string Nombre { get; set; } = null!;

    }
}
