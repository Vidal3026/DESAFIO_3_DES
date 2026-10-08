using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RecetasApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ReportesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/Reportes  -> reportes de SSRS disponibles y la URL de su visor
        // La URL apunta a /ReportServer de esta misma aplicación (proxy hacia SSRS).
        // rs:Embed=true muestra el visor de SSRS sin el marco del portal.
        [HttpGet]
        public IActionResult GetReportes()
        {
            var carpeta = _configuration["Ssrs:CarpetaReportes"] ?? "/Recetas";

            string Url(string reporte) =>
                $"/ReportServer/Pages/ReportViewer.aspx?{Uri.EscapeDataString($"{carpeta}/{reporte}")}&rs:Command=Render&rs:Embed=true";

            return Ok(new[]
            {
                new
                {
                    id = "recetas-ingredientes",
                    nombre = "Recetas e ingredientes",
                    descripcion = "Lista de recetas con sus ingredientes, filtradas por tiempo de preparación.",
                    url = Url("Reporte1_RecetasIngredientes"),
                    parametro = (string?)"TiempoMaximo"
                },
                new
                {
                    id = "pasos-por-receta",
                    nombre = "Pasos por receta",
                    descripcion = "Cantidad de pasos de cada receta y el orden en que deben realizarse.",
                    url = Url("Reporte2_PasosPorReceta"),
                    parametro = (string?)null
                }
            });
        }
    }
}
