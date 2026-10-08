using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using RecetasApi.Data;
using RecetasApi.Models;
using RecetasApi.Reportes;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de datos (SQL Server) con Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Autenticación con ASP.NET Core Identity (endpoints /register, /login, etc.) + roles
builder.Services.AddIdentityApiEndpoints<Usuario>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<ErroresIdentityEs>();

// 3. Autorización: política que solo permite al rol Administrador
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloAdministrador", policy => policy.RequireRole(SeedIdentity.RolAdministrador));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Proxy hacia SQL Server Reporting Services (visor de reportes)
builder.Services.AddSsrsProxy();

var app = builder.Build();

// 5. Aplica migraciones y crea roles y usuarios de prueba al iniciar
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await SeedIdentity.InicializarAsync(scope.ServiceProvider);
}

app.UseSwagger();
app.UseSwaggerUI();

// Aplicación web (wwwroot/index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints de Identity: /register, /login, /refresh, /manage/info, ...
// Después de un registro exitoso se asigna automáticamente el rol "Usuario".
app.MapIdentityApi<Usuario>().AddEndpointFilter(async (context, next) =>
{
    var resultado = await next(context);

    if (context.HttpContext.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase)
        && resultado is Results<Ok, ValidationProblem> { Result: Ok }
        && context.Arguments.OfType<RegisterRequest>().FirstOrDefault() is { } registro)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
        var usuario = await userManager.FindByEmailAsync(registro.Email);
        if (usuario != null)
        {
            await userManager.AddToRoleAsync(usuario, SeedIdentity.RolUsuario);
        }
    }

    return resultado;
});

app.MapControllers();

// Visor de reportes de SSRS servido desde /ReportServer (requiere sesión iniciada)
app.MapSsrsProxy();

app.Run();
