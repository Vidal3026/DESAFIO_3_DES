using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecetasApi.Models;

namespace RecetasApi.Data
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Receta> Recetas { get; set; }
        public DbSet<Ingrediente> Ingredientes { get; set; }
        public DbSet<PasoPreparacion> PasosPreparacion { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Necesario para que se creen las tablas de Identity
            base.OnModelCreating(modelBuilder);

            // Relaciones: una receta tiene muchos ingredientes y muchos pasos
            modelBuilder.Entity<Ingrediente>()
                .HasOne(i => i.Receta)
                .WithMany(r => r.Ingredientes)
                .HasForeignKey(i => i.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasoPreparacion>()
                .HasOne(p => p.Receta)
                .WithMany(r => r.Pasos)
                .HasForeignKey(p => p.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== DATA SEED =====
            modelBuilder.Entity<Receta>().HasData(
                new Receta { Id = 1, Nombre = "Ensalada César", Descripcion = "Ensalada clásica con pollo, lechuga y aderezo César.", TiempoPreparacion = 20 },
                new Receta { Id = 2, Nombre = "Pasta Carbonara", Descripcion = "Pasta con salsa de crema, huevo y queso parmesano.", TiempoPreparacion = 30 },
                new Receta { Id = 3, Nombre = "Sopa de Tomate", Descripcion = "Sopa ligera de tomate con albahaca.", TiempoPreparacion = 40 }
            );

            modelBuilder.Entity<Ingrediente>().HasData(
                // Receta 1 - Ensalada César
                new Ingrediente { Id = 1, Nombre = "Lechuga romana", Cantidad = 1, UnidadMedida = "Unidad", RecetaId = 1 },
                new Ingrediente { Id = 2, Nombre = "Pollo a la parrilla", Cantidad = 200, UnidadMedida = "Gramos", RecetaId = 1 },
                new Ingrediente { Id = 3, Nombre = "Aderezo César", Cantidad = 50, UnidadMedida = "Mililitros", RecetaId = 1 },
                // Receta 2 - Pasta Carbonara
                new Ingrediente { Id = 4, Nombre = "Pasta espagueti", Cantidad = 250, UnidadMedida = "Gramos", RecetaId = 2 },
                new Ingrediente { Id = 5, Nombre = "Crema de leche", Cantidad = 100, UnidadMedida = "Mililitros", RecetaId = 2 },
                new Ingrediente { Id = 6, Nombre = "Huevo", Cantidad = 1, UnidadMedida = "Unidad", RecetaId = 2 },
                new Ingrediente { Id = 7, Nombre = "Queso parmesano", Cantidad = 50, UnidadMedida = "Gramos", RecetaId = 2 },
                // Receta 3 - Sopa de Tomate
                new Ingrediente { Id = 8, Nombre = "Tomates frescos", Cantidad = 500, UnidadMedida = "Gramos", RecetaId = 3 },
                new Ingrediente { Id = 9, Nombre = "Albahaca", Cantidad = 5, UnidadMedida = "Hojas", RecetaId = 3 }
            );

            modelBuilder.Entity<PasoPreparacion>().HasData(
                // Receta 1 - Ensalada César
                new PasoPreparacion { Id = 1, Orden = 1, Descripcion = "Lavar la lechuga romana y cortarla en trozos.", RecetaId = 1 },
                new PasoPreparacion { Id = 2, Orden = 2, Descripcion = "Asar el pollo a la parrilla y cortarlo en tiras.", RecetaId = 1 },
                new PasoPreparacion { Id = 3, Orden = 3, Descripcion = "Mezclar la lechuga, el pollo y el aderezo César.", RecetaId = 1 },
                // Receta 2 - Pasta Carbonara
                new PasoPreparacion { Id = 4, Orden = 1, Descripcion = "Cocinar la pasta en agua hirviendo con sal.", RecetaId = 2 },
                new PasoPreparacion { Id = 5, Orden = 2, Descripcion = "Mezclar el huevo, la crema y el queso parmesano.", RecetaId = 2 },
                new PasoPreparacion { Id = 6, Orden = 3, Descripcion = "Añadir la mezcla a la pasta caliente.", RecetaId = 2 },
                // Receta 3 - Sopa de Tomate
                new PasoPreparacion { Id = 7, Orden = 1, Descripcion = "Cortar los tomates y hervirlos hasta que se ablanden.", RecetaId = 3 },
                new PasoPreparacion { Id = 8, Orden = 2, Descripcion = "Licuar los tomates y agregar la albahaca.", RecetaId = 3 },
                new PasoPreparacion { Id = 9, Orden = 3, Descripcion = "Cocinar por 10 minutos más y servir caliente.", RecetaId = 3 }
            );
        }
    }
}
