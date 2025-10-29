using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesaListo.Models
{
    public class Mesa
    {
        public Mesa()
        {
            Reservas = new HashSet<Reserva>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; }

        [Required]
        [Range(1, 20)]
        public int Capacidad { get; set; }

        [Required]
        public int RestauranteId { get; set; }

        [ForeignKey("RestauranteId")]
        public virtual Restaurante? Restaurante { get; set; }

        public virtual ICollection<Reserva> Reservas { get; set; }

        // AGREGAR esta propiedad para mostrar información en dropdowns
        [NotMapped]
        public string DisplayInfo => $"{Codigo} - {Restaurante?.Nombre} (Capacidad: {Capacidad})";
    }
}