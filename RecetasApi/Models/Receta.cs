using System.ComponentModel.DataAnnotations;

namespace RecetasApi.Models
{
    public class Receta
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la receta es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        // Tiempo estimado de preparación en minutos
        [Range(1, 1440, ErrorMessage = "El tiempo de preparación debe estar entre 1 y 1440 minutos.")]
        public int TiempoPreparacion { get; set; }

        public List<Ingrediente> Ingredientes { get; set; } = new();

        public List<PasoPreparacion> Pasos { get; set; } = new();
    }
}
