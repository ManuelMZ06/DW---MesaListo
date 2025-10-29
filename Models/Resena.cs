using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesaListo.Models
{
    public class Resena
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Puntuacion { get; set; }

        [StringLength(500)]
        public string? Comentario { get; set; } // Hacer nullable

        [Required]
        public int ReservaId { get; set; }

        [Required]
        public string ClienteId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Navigation properties - hacer nullable
        [ForeignKey("ReservaId")]
        public virtual Reserva? Reserva { get; set; }

        [ForeignKey("ClienteId")]
        public virtual IdentityUser? Cliente { get; set; }
    }
}