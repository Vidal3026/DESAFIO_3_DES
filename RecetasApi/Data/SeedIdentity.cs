using Microsoft.AspNetCore.Identity;
using RecetasApi.Models;

namespace RecetasApi.Data
{
    public static class SeedIdentity
    {
        public const string RolAdministrador = "Administrador";
        public const string RolUsuario = "Usuario";

        // Crea los roles y un usuario de prueba para cada rol (solo si no existen)
        public static async Task InicializarAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<Usuario>>();

            foreach (var rol in new[] { RolAdministrador, RolUsuario })
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            await CrearUsuarioAsync(userManager, "admin@recetas.com", "Admin123!", RolAdministrador);
            await CrearUsuarioAsync(userManager, "usuario@recetas.com", "Usuario123!", RolUsuario);
        }

        private static async Task CrearUsuarioAsync(UserManager<Usuario> userManager, string email, string password, string rol)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                usuario = new Usuario { UserName = email, Email = email, EmailConfirmed = true };
                await userManager.CreateAsync(usuario, password);
            }

            if (!await userManager.IsInRoleAsync(usuario, rol))
            {
                await userManager.AddToRoleAsync(usuario, rol);
            }
        }
    }
}
