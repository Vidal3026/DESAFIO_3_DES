using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecetasApi.Data;
using RecetasApi.Models;

namespace RecetasApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CuentaController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public CuentaController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: api/Cuenta/yo  -> correo y roles del usuario conectado
        [HttpGet("yo")]
        public IActionResult Yo()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return Ok(new { email = User.Identity?.Name, roles });
        }

        // POST: api/Cuenta/logout  -> cierra la sesión (borra la cookie)
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        // GET: api/Cuenta/usuarios  (solo Administrador) -> usuarios registrados y sus roles
        [HttpGet("usuarios")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var resultado = new List<object>();
            foreach (var usuario in usuarios)
            {
                resultado.Add(new { email = usuario.Email, roles = await _userManager.GetRolesAsync(usuario) });
            }
            return Ok(resultado);
        }

        // POST: api/Cuenta/asignar-rol  (solo Administrador) -> deja al usuario solo con el rol indicado
        [HttpPost("asignar-rol")]
        [Authorize(Policy = "SoloAdministrador")]
        public async Task<IActionResult> AsignarRol(AsignarRolDto dto)
        {
            if (dto.Rol != SeedIdentity.RolAdministrador && dto.Rol != SeedIdentity.RolUsuario)
            {
                return BadRequest("El rol debe ser Administrador o Usuario.");
            }

            if (string.Equals(dto.Email, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("No puede cambiar su propio rol.");
            }

            var usuario = await _userManager.FindByEmailAsync(dto.Email);
            if (usuario == null)
            {
                return NotFound("No existe un usuario con ese correo.");
            }

            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            await _userManager.AddToRoleAsync(usuario, dto.Rol);

            return Ok($"Rol {dto.Rol} asignado a {dto.Email}.");
        }
    }
}
