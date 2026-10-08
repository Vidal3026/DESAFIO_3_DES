using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecetasApi.Data;
using RecetasApi.Models;

namespace RecetasApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PasosPreparacionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PasosPreparacionController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PasosPreparacion  (opcional: ?recetaId=1), ordenados por receta y orden
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PasoPreparacion>>> GetPasos(int? recetaId)
        {
            var consulta = _context.PasosPreparacion.AsQueryable();
            if (recetaId.HasValue)
            {
                consulta = consulta.Where(p => p.RecetaId == recetaId.Value);
            }
            return await consulta.OrderBy(p => p.RecetaId).ThenBy(p => p.Orden).ToListAsync();
        }

        // GET: api/PasosPreparacion/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PasoPreparacion>> GetPaso(int id)
        {
            var paso = await _context.PasosPreparacion.FindAsync(id);
            if (paso == null)
            {
                return NotFound();
            }
            return paso;
        }

        // POST: api/PasosPreparacion  (solo Administrador)
        [HttpPost]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<ActionResult<PasoPreparacion>> PostPaso(PasoPreparacion paso)
        {
            if (!await _context.Recetas.AnyAsync(r => r.Id == paso.RecetaId))
            {
                return BadRequest("La receta asociada no existe.");
            }

            _context.PasosPreparacion.Add(paso);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaso), new { id = paso.Id }, paso);
        }

        // PUT: api/PasosPreparacion/5  (solo Administrador)
        [HttpPut("{id}")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> PutPaso(int id, PasoPreparacion paso)
        {
            if (id != paso.Id)
            {
                return BadRequest("El id de la URL no coincide con el id del cuerpo.");
            }

            if (!await _context.Recetas.AnyAsync(r => r.Id == paso.RecetaId))
            {
                return BadRequest("La receta asociada no existe.");
            }

            var existente = await _context.PasosPreparacion.FindAsync(id);
            if (existente == null)
            {
                return NotFound();
            }

            existente.Descripcion = paso.Descripcion;
            existente.Orden = paso.Orden;
            existente.RecetaId = paso.RecetaId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/PasosPreparacion/5  (solo Administrador)
        [HttpDelete("{id}")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> DeletePaso(int id)
        {
            var paso = await _context.PasosPreparacion.FindAsync(id);
            if (paso == null)
            {
                return NotFound();
            }

            _context.PasosPreparacion.Remove(paso);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
