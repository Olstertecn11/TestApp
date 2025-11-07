using Microsoft.EntityFrameworkCore;
using TestAplication.Models.Data;

namespace TestAplication.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pais> Paises { get; set; }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Colaborador> Colaboradores { get; set; }
        public DbSet<HistorialCarga> HistorialCargas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================
            // SCHEMAS
            // ======================
            modelBuilder.Entity<Pais>().ToTable("Pais", "maestra");
            modelBuilder.Entity<Empresa>().ToTable("Empresa", "rrhh");
            modelBuilder.Entity<Sucursal>().ToTable("Sucursal", "rrhh");
            modelBuilder.Entity<Colaborador>().ToTable("Colaborador", "rrhh");
            modelBuilder.Entity<HistorialCarga>().ToTable("HistorialCarga", "rrhh");

            // ======================
            //  RELACIONES PRINCIPALES
            // ======================

            modelBuilder.Entity<Sucursal>()
                .HasOne(s => s.Empresa)
                .WithMany(e => e.Sucursales)
                .HasForeignKey(s => s.IdEmpresaFk)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Colaborador>()
                .HasOne(c => c.Sucursal)
                .WithMany(s => s.Colaboradores)
                .HasForeignKey(c => c.IdSucursalFk)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================
            // HISTORIAL DE CARGA RELACIONES
            // ======================

            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.HistorialCarga)
                .WithMany(h => h.Empresas)
                .HasForeignKey(e => e.IdHistorialCargaFk)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Sucursal>()
                .HasOne<HistorialCarga>()
                .WithMany()
                .HasForeignKey(s => s.IdHistorialCargaFk)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Colaborador>()
                .HasOne<HistorialCarga>()
                .WithMany()
                .HasForeignKey(c => c.IdHistorialCargaFk)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
