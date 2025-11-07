using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TestAplication.Models.Data
{
    public class HistorialCarga
    {
        [Key]
        public int IdHistorialCarga { get; set; }

        [Required]
        [StringLength(255)]
        public string NombreArchivo { get; set; } = null!;

        [Required]
        public DateTime FechaCarga { get; set; } = DateTime.Now;

        public int TotalEmpresas { get; set; } = 0;
        public int TotalSucursales { get; set; } = 0;
        public int TotalColaboradores { get; set; } = 0;

        [StringLength(50)]
        public string Estado { get; set; } = "Completado";

        public string? MensajeError { get; set; }

        [StringLength(50)]
        public string? IpCarga { get; set; }

        // Relación con Empresa
        public ICollection<Empresa>? Empresas { get; set; }
    }
}
