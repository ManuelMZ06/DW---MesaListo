using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesaListo.Models
{
    public class Restaurante
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(200)]
        public string Direccion { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefono { get; set; }

        // Relación con el usuario propietario
        public string? UsuarioId { get; set; }

        // Navigation properties
        public virtual ICollection<Mesa>? Mesas { get; set; }
    }
}