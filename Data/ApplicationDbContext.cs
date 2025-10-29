using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MesaListo.Models;

namespace MesaListo.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Restaurante> Restaurantes { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Resena> Resenas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar el esquema por defecto
            builder.HasDefaultSchema("public");

            // Configurar DateTime para PostgreSQL
            builder.Entity<Reserva>()
                .Property(r => r.FechaHora)
                .HasConversion(
                    v => v.ToUniversalTime(), // Convertir a UTC al guardar
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc) // Especificar como UTC al leer
                );

            // Configuraciones adicionales
            builder.Entity<Reserva>()
                .HasIndex(r => new { r.MesaId, r.FechaHora })
                .IsUnique();
        }
    }
}