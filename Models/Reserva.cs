using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesaListo.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime FechaHora { get; set; }

        [Required]
        public int MesaId { get; set; }

        [Required]
        public string ClienteId { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        // Navigation properties - hacer nullable
        [ForeignKey("MesaId")]
        public virtual Mesa? Mesa { get; set; }

        [ForeignKey("ClienteId")]
        public virtual IdentityUser? Cliente { get; set; } // Hacer nullable

        public virtual Resena? Resena { get; set; }
    }
}