using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RecetasApi.Models
{
    public class Ingrediente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del ingrediente es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0.01, 100000, ErrorMessage = "La cantidad debe ser mayor que 0.")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
        [StringLength(30)]
        public string UnidadMedida { get; set; } = string.Empty;

        public int RecetaId { get; set; }

        [JsonIgnore]
        public Receta? Receta { get; set; }
    }
}
