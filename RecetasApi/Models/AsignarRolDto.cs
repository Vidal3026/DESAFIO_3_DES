using System.ComponentModel.DataAnnotations;

namespace RecetasApi.Models
{
    public class AsignarRolDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = string.Empty;
    }
}
