# Guía para la defensa y guion del video

## Antes de empezar (5 minutos antes)
1. Verifica que estén corriendo los servicios **SQL Server (MSSQLSERVER)** y **SQL Server Reporting Services** (en `services.msc`).
2. Abre `http://localhost/Reports` y confirma que existe la carpeta **Recetas** con los 2 reportes.
3. Abre `RecetasApp.slnx` en Visual Studio → perfil **http** → **F5**.
4. Ten abiertos SSMS (base `RecetasDB`) y Swagger (`http://localhost:5080/swagger`).

---

## Demostración sugerida (en este orden)

### 1. Comprensión del problema (20%)
Explica en una frase qué hace el sistema: gestiona recetas, ingredientes y pasos; usa Identity para la seguridad y SSRS para los reportes. Muestra la tabla del punto 11 del README.

### 2. Base de datos y EF Core
- En SSMS, abre `RecetasDB` y muestra:
  - Las tablas `Recetas`, `Ingredientes` y `PasosPreparacion`, con sus claves foráneas.
  - Las tablas `AspNet*` de Identity.
- Muestra que los datos del enunciado ya están cargados (data seed): `SELECT * FROM Recetas`.
- En código, muestra `AppDbContext.cs`:
  - Hereda de `IdentityDbContext<Usuario>`.
  - Define las relaciones con `HasOne/WithMany` y borrado en cascada.
  - Carga los datos con `HasData`.

### 3. Identity: autenticación y autorización (40%)
1. **Sin sesión:** en Swagger, `GET /api/Recetas` responde **401**.
2. **Registro:** en la app, pestaña *Registrarse*.
   - Muestra que una contraseña débil da errores en español.
   - Crea la cuenta y verás que entra con el rol **Usuario**.
3. **Usuario:**
   - Puede ver recetas, ingredientes y pasos.
   - **No** ve los botones Nueva, Editar ni Eliminar, ni el menú Usuarios.
   - Para demostrar que la API también protege, ejecuta en la consola del navegador (F12):
     ```js
     fetch('/api/Recetas/1', { method: 'DELETE' }).then(r => r.status)   // 403
     ```
4. **Administrador** (`admin@recetas.com` / `Admin123!`):
   - Crea una receta. Primero con nombre "Ab" para mostrar la validación (3 a 100 caracteres).
   - Agrega un ingrediente. Prueba la validación (3 a 50 caracteres y cantidad mayor que 0).
   - Agrega un paso. Prueba la validación (mínimo 10 caracteres).
   - Edita y elimina.
   - Menú **Usuarios**: cambia el rol del usuario que registraste a Administrador.
5. **En código:**
   - `Program.cs`: `AddIdentityApiEndpoints`, `AddRoles`, la política `SoloAdministrador` y `MapIdentityApi`.
   - Controladores: `[Authorize]` en la clase y `[Authorize(Policy = "SoloAdministrador")]` en POST, PUT y DELETE.
   - `SeedIdentity.cs`: crea los roles y los usuarios.

### 4. Reportes con SSRS (30%)
1. En la app, pestaña **Reportes**:
   - **Recetas e ingredientes:** cambia el tiempo máximo a 20 (1 receta), luego a 30 (2) y a 60 (3). También funciona desde la barra de parámetros del visor ("Ver informe").
   - **Pasos por receta:** muestra la cantidad de pasos de cada receta y el orden ("Paso 1 de 3"…).
   - Exporta a **PDF** desde el visor.
2. En `http://localhost/Reports`, muestra los reportes publicados en la carpeta Recetas.
3. Abre un `.rdl` en **Report Builder** y muestra:
   - El dataset (consulta SQL con `@TiempoMaximo`).
   - El parámetro.
   - El agrupamiento por receta.
4. Explica la integración: el visor de SSRS se carga en un iframe desde `/ReportServer` de la propia aplicación (`SsrsProxy.cs`). Si te preguntan por qué, responde:
   > SSRS envía `X-Frame-Options: SAMEORIGIN` y no deja incrustarse desde otro puerto. El proxy lo resuelve y además exige iniciar sesión con Identity para ver los reportes.

### 5. Documentación (10%)
Muestra el README: arquitectura, instalación, endpoints y decisiones técnicas.

---

## Preguntas probables y respuestas cortas

| Pregunta | Respuesta |
|---|---|
| ¿Qué diferencia hay entre autenticación y autorización? | La autenticación verifica **quién eres** (login con cookie). La autorización decide **qué puedes hacer** (roles y políticas) |
| ¿Dónde se guardan los usuarios y roles? | En SQL Server, en las tablas `AspNetUsers`, `AspNetRoles` y `AspNetUserRoles`, creadas por la migración de Identity |
| ¿Cómo sabe la API el rol del usuario? | El rol va como *claim* dentro de la cookie de autenticación. `[Authorize(Policy=...)]` lo revisa en cada petición |
| ¿Qué pasa si un Usuario llama directamente a DELETE? | La API responde **403 Forbidden**. Ocultar botones es solo de interfaz; la seguridad está en el servidor |
| ¿Por qué una política y no solo `Roles="Administrador"`? | La regla queda definida en un solo lugar. Si cambia, se modifica solo en `Program.cs` |
| ¿Cómo se validan los campos? | Con DataAnnotations (`[Required]`, `[StringLength]`, `[MinLength]`). `[ApiController]` responde 400 automáticamente |
| ¿Qué es el data seed? | Datos iniciales definidos con `HasData` en `OnModelCreating`. Se insertan al aplicar la migración |
| ¿Cómo filtra el Reporte 1? | Con el parámetro `@TiempoMaximo` en la consulta: `WHERE TiempoPreparacion <= @TiempoMaximo` |
| ¿Cómo cuenta los pasos el Reporte 2? | Con `COUNT(p.Id) OVER (PARTITION BY r.Id)` y agrupando por receta, ordenado por `Orden` |
| ¿Qué pasa al borrar una receta? | Se eliminan en cascada sus ingredientes y pasos (`OnDelete(DeleteBehavior.Cascade)`) |

---

## Guion del video (5 a 7 minutos)

| Tiempo | Qué mostrar | Qué decir (resumen) |
|---|---|---|
| 0:00–0:30 | Portada / README | Integrantes, objetivo del sistema y tecnologías (.NET 9, Identity, EF Core, SQL Server, SSRS) |
| 0:30–1:15 | Estructura del proyecto en Visual Studio | Modelos, controladores, `AppDbContext`, `Program.cs`, carpeta `SSRS` |
| 1:15–2:00 | SSMS: tablas y data seed | Relaciones 1 a muchos, tablas de Identity, datos iniciales del enunciado |
| 2:00–3:30 | App: login, registro, Usuario vs Administrador | Usuario solo lee (y recibe 403 al forzar DELETE). Administrador hace el CRUD; validaciones; gestión de roles |
| 3:30–4:00 | Código de seguridad | `[Authorize]`, política `SoloAdministrador`, `SeedIdentity` |
| 4:00–5:30 | Reportes en la app | Reporte 1 con filtro de tiempo (20/30/60); Reporte 2 con cantidad y orden de pasos; exportar a PDF |
| 5:30–6:00 | Portal de SSRS y Report Builder | Reportes publicados, consulta y parámetro |
| 6:00–6:30 | Cierre | Resumen de cómo se cumple cada requisito |

---

## Reparto sugerido de commits (uno por integrante)

1. **Integrante 1:** `Models/`, `Data/AppDbContext.cs` (relaciones y data seed), `Migrations/`.
2. **Integrante 2:** `Program.cs`, `Data/SeedIdentity.cs`, `Data/ErroresIdentityEs.cs` y `Controllers/` (Identity, roles y CRUD).
3. **Integrante 3:** `SSRS/` (reportes y scripts), `Reportes/SsrsProxy.cs`, `wwwroot/`, `README.md` y `docs/`.

Cada integrante hace commit desde **su propia cuenta** (con `git config user.name` y `user.email` configurados con sus datos).
