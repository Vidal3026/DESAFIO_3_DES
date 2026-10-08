using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecetasApi.Data;
using RecetasApi.Models;

namespace RecetasApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Solo usuarios autenticados
    public class RecetasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecetasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Recetas  (cualquier usuario autenticado; opcional: ?tiempoMaximo=30)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Receta>>> GetRecetas(int? tiempoMaximo)
        {
            var consulta = _context.Recetas.AsQueryable();
            if (tiempoMaximo.HasValue)
            {
                consulta = consulta.Where(r => r.TiempoPreparacion <= tiempoMaximo.Value);
            }
            return await consulta.OrderBy(r => r.Nombre).ToListAsync();
        }

        // GET: api/Recetas/5  (incluye ingredientes y pasos en orden)
        [HttpGet("{id}")]
        public async Task<ActionResult<Receta>> GetReceta(int id)
        {
            var receta = await _context.Recetas
                .Include(r => r.Ingredientes)
                .Include(r => r.Pasos.OrderBy(p => p.Orden))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receta == null)
            {
                return NotFound();
            }

            return receta;
        }

        // POST: api/Recetas  (solo Administrador)
        [HttpPost]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<ActionResult<Receta>> PostReceta(Receta receta)
        {
            _context.Recetas.Add(receta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReceta), new { id = receta.Id }, receta);
        }

        // PUT: api/Recetas/5  (solo Administrador)
        [HttpPut("{id}")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> PutReceta(int id, Receta receta)
        {
            if (id != receta.Id)
            {
                return BadRequest("El id de la URL no coincide con el id del cuerpo.");
            }

            var existente = await _context.Recetas.FindAsync(id);
            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre = receta.Nombre;
            existente.Descripcion = receta.Descripcion;
            existente.TiempoPreparacion = receta.TiempoPreparacion;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Recetas/5  (solo Administrador; borra también sus ingredientes y pasos)
        [HttpDelete("{id}")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> DeleteReceta(int id)
        {
            var receta = await _context.Recetas.FindAsync(id);
            if (receta == null)
            {
                return NotFound();
            }

            _context.Recetas.Remove(receta);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
