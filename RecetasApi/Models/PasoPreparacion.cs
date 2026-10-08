using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RecetasApi.Models
{
    public class PasoPreparacion
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La descripción del paso es obligatoria.")]
        [MinLength(10, ErrorMessage = "La descripción debe tener al menos 10 caracteres.")]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "El orden debe ser mayor que 0.")]
        public int Orden { get; set; }

        public int RecetaId { get; set; }

        [JsonIgnore]
        public Receta? Receta { get; set; }
    }
}
