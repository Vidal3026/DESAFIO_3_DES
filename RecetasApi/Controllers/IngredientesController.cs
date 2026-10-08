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
    public class IngredientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IngredientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Ingredientes  (opcional: ?recetaId=1)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ingrediente>>> GetIngredientes(int? recetaId)
        {
            var consulta = _context.Ingredientes.AsQueryable();
            if (recetaId.HasValue)
            {
                consulta = consulta.Where(i => i.RecetaId == recetaId.Value);
            }
            return await consulta.ToListAsync();
        }

        // GET: api/Ingredientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Ingrediente>> GetIngrediente(int id)
        {
            var ingrediente = await _context.Ingredientes.FindAsync(id);
            if (ingrediente == null)
            {
                return NotFound();
            }
            return ingrediente;
        }

        // POST: api/Ingredientes  (solo Administrador)
        [HttpPost]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<ActionResult<Ingrediente>> PostIngrediente(Ingrediente ingrediente)
        {
            if (!await _context.Recetas.AnyAsync(r => r.Id == ingrediente.RecetaId))
            {
                return BadRequest("La receta asociada no existe.");
            }

            _context.Ingredientes.Add(ingrediente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetIngrediente), new { id = ingrediente.Id }, ingrediente);
        }

        // PUT: api/Ingredientes/5  (solo Administrador)
        [HttpPut("{id}")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> PutIngrediente(int id, Ingrediente ingrediente)
        {
            if (id != ingrediente.Id)
            {
                return BadRequest("El id de la URL no coincide con el id del cuerpo.");
            }

            if (!await _context.Recetas.AnyAsync(r => r.Id == ingrediente.RecetaId))
            {
                return BadRequest("La receta asociada no existe.");
            }

            var existente = await _context.Ingredientes.FindAsync(id);
            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre = ingrediente.Nombre;
            existente.Cantidad = ingrediente.Cantidad;
            existente.UnidadMedida = ingrediente.UnidadMedida;
            existente.RecetaId = ingrediente.RecetaId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Ingredientes/5  (solo Administrador)
        [HttpDelete("{id}")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> DeleteIngrediente(int id)
        {
            var ingrediente = await _context.Ingredientes.FindAsync(id);
            if (ingrediente == null)
            {
                return NotFound();
            }

            _context.Ingredientes.Remove(ingrediente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
