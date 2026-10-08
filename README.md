# Recetario: Desafío práctico #3

**Desarrollo de Software Empresarial (G01L), Universidad Don Bosco, ciclo 02-2026**

Sistema para gestionar **recetas de cocina**, sus **ingredientes** y sus **pasos de preparación**. Incluye:

- Una **API REST en .NET 9**.
- Autenticación y autorización con **ASP.NET Core Identity**: roles *Administrador* y *Usuario*.
- Persistencia con **Entity Framework Core + SQL Server**, con data seed.
- Una **aplicación web** que consume la API.
- Dos **reportes de SQL Server Reporting Services (SSRS)** que se ven dentro de la aplicación con el visor de SSRS.

---

## 1. Tecnologías

| Capa | Tecnología |
|---|---|
| API y servidor web | ASP.NET Core 9.0 (controladores Web API) |
| Seguridad | ASP.NET Core Identity (`AddIdentityApiEndpoints` + roles + política de autorización) |
| Datos | Entity Framework Core 9 + SQL Server (migraciones y `HasData`) |
| Reportes | SQL Server Reporting Services 2022 (archivos `.rdl`) |
| Integración con el visor | YARP (`Yarp.ReverseProxy`), que reenvía `/ReportServer` hacia SSRS |
| Aplicación web | HTML + Bootstrap 5 + JavaScript (`wwwroot`) |
| Documentación de la API | Swagger (Swashbuckle) en `/swagger` |

---

## 2. Arquitectura

```
 Navegador (http://localhost:5080)
   │
   │  index.html + app.js  ──fetch──►  /api/...           (cookie de Identity)
   │  <iframe> visor SSRS  ─────────►  /ReportServer/...  (cookie de Identity)
   ▼
 ┌──────────────────────── RecetasApi (ASP.NET Core 9) ────────────────────────┐
 │  Identity (/register, /login)   [Authorize] + política "SoloAdministrador"  │
 │  Controladores: Recetas · Ingredientes · PasosPreparacion · Cuenta · Reportes│
 │  AppDbContext (EF Core) ─────────────────────► SQL Server (RecetasDB)        │
 │  SsrsProxy (YARP, credenciales de Windows) ──► SSRS http://localhost/ReportServer
 └──────────────────────────────────────────────────────────────────────────────┘
                                                   SSRS lee los datos de RecetasDB
```

**Flujo de una petición:**
1. El usuario inicia sesión en `/login?useCookies=true`. Identity crea una **cookie de autenticación** que incluye los **roles** del usuario.
2. Cada petición a `/api/...` pasa por `[Authorize]`. Las operaciones de crear, editar y eliminar exigen además la política **SoloAdministrador**, que pide el rol Administrador.
3. Los reportes se cargan en un `<iframe>` desde `/ReportServer/...` **de la misma aplicación**. El proxy exige sesión iniciada y reenvía la petición a SSRS con la cuenta de Windows del proceso.

---

## 3. Estructura de carpetas

```
DESAFIO_3_DES/
├── RecetasApp.slnx                  ← solución (abrir con Visual Studio)
├── RecetasApi/                      ← proyecto ASP.NET Core 9
│   ├── Controllers/
│   │   ├── RecetasController.cs             CRUD de recetas
│   │   ├── IngredientesController.cs        CRUD de ingredientes
│   │   ├── PasosPreparacionController.cs    CRUD de pasos
│   │   ├── CuentaController.cs              sesión, usuarios y roles
│   │   └── ReportesController.cs            lista de reportes y URL del visor
│   ├── Data/
│   │   ├── AppDbContext.cs                  IdentityDbContext + relaciones + data seed
│   │   ├── SeedIdentity.cs                  roles y usuarios de prueba
│   │   └── ErroresIdentityEs.cs             mensajes de Identity en español
│   ├── Migrations/                          migración inicial de EF Core
│   ├── Models/                              Receta, Ingrediente, PasoPreparacion, Usuario, AsignarRolDto
│   ├── Reportes/SsrsProxy.cs                proxy hacia SSRS (visor integrado)
│   ├── wwwroot/                             aplicación web (index.html, css, js)
│   ├── Program.cs                           configuración (EF, Identity, autorización, proxy)
│   └── appsettings.json                     cadena de conexión y URL de SSRS
└── SSRS/
    ├── Reporte1_RecetasIngredientes.rdl     Reporte 1
    ├── Reporte2_PasosPorReceta.rdl          Reporte 2
    ├── consultas-reportes.sql               consultas SQL de los reportes
    ├── publicar-reportes.ps1                publica los .rdl en SSRS
    └── dar-permisos-ssrs.ps1                da permisos en SSRS a un usuario de Windows
```

---

## 4. Modelo de datos y validaciones

| Entidad | Campos | Validaciones (DataAnnotations) |
|---|---|---|
| **Receta** | Id, Nombre, Descripcion, TiempoPreparacion (min) | Nombre obligatorio, **3 a 100** caracteres; tiempo de 1 a 1440 |
| **Ingrediente** | Id, Nombre, Cantidad, UnidadMedida, RecetaId (FK) | Nombre obligatorio, **3 a 50** caracteres; cantidad > 0; unidad obligatoria; la receta debe existir |
| **PasoPreparacion** | Id, Descripcion, Orden, RecetaId (FK) | Descripción obligatoria, **mínimo 10** caracteres; orden ≥ 1; la receta debe existir |

- Relaciones **1 a muchos**: Receta → Ingredientes y Receta → Pasos, con **borrado en cascada**. Al eliminar una receta se eliminan sus ingredientes y pasos.
- `[ApiController]` valida automáticamente y responde **400** con los mensajes de error.
- **Data seed** (`HasData` en `AppDbContext`): las 3 recetas, 9 ingredientes y 9 pasos del enunciado.
  - El tiempo se guarda como **número de minutos** (20, 30, 40) para poder filtrarlo en el Reporte 1.

---

## 5. Autenticación y autorización (ASP.NET Core Identity)

| Elemento | Implementación |
|---|---|
| Usuario | `Usuario : IdentityUser`; `AppDbContext : IdentityDbContext<Usuario>` (tablas `AspNet*`) |
| Endpoints de Identity | `app.MapIdentityApi<Usuario>()` → `/register`, `/login`, `/refresh`, `/manage/info`… |
| Sesión | Cookie: `/login?useCookies=true&useSessionCookies=true` |
| Roles | **Administrador** y **Usuario**, creados al iniciar (`SeedIdentity`) |
| Rol por defecto | Al registrarse por `/register`, se asigna **Usuario** automáticamente (filtro en `Program.cs`) |
| Lectura | `[Authorize]` en todos los controladores: solo usuarios autenticados |
| Escritura | `[Authorize(Policy = "SoloAdministrador")]` en POST, PUT y DELETE |
| Gestión de roles | `GET /api/Cuenta/usuarios` y `POST /api/Cuenta/asignar-rol` (solo Administrador) |
| Interfaz | Los botones de crear, editar y eliminar y el menú *Usuarios* solo se muestran al Administrador. **La seguridad real está en la API**: aunque alguien fuerce la petición, recibe **403** |

**Usuarios de prueba** (los crea la aplicación al iniciar):

| Correo | Contraseña | Rol |
|---|---|---|
| admin@recetas.com | Admin123! | Administrador |
| usuario@recetas.com | Usuario123! | Usuario |

**Códigos de respuesta:**
- **401**: sin sesión.
- **403**: con sesión pero sin el rol Administrador.
- **400**: datos inválidos.

---

## 6. Reportes con SSRS

| Reporte | Contenido | Parámetro |
|---|---|---|
| **Reporte1_RecetasIngredientes** | Recetas agrupadas con sus ingredientes (cantidad y unidad) y el número de ingredientes de cada receta | `TiempoMaximo` (minutos, por defecto 60): muestra las recetas con ese tiempo **o menor** |
| **Reporte2_PasosPorReceta** | Cada receta con su **cantidad total de pasos** y los pasos **en orden** ("Paso 1 de 3", "Paso 2 de 3"…) | — |

- Fuente de datos: `RecetasDB` en `localhost`, con seguridad integrada de Windows. Las consultas están en `SSRS/consultas-reportes.sql`.
- Se publican en la carpeta **/Recetas** del servidor de reportes, con `SSRS/publicar-reportes.ps1`.
- Los `.rdl` se pueden abrir y editar con **Report Builder**: Archivo → Abrir.

**Cómo se integran en la aplicación web:**
- La pestaña **Reportes** carga el **visor de SSRS** (`ReportViewer.aspx` con `rs:Embed=true`) en un `<iframe>`.
- El visor permite navegar páginas, cambiar el parámetro, imprimir y exportar a PDF, Excel o Word.
- SSRS envía la cabecera `X-Frame-Options: SAMEORIGIN`, que **impide** incrustarlo desde otro origen (la app usa el puerto 5080 y SSRS el 80). Por eso la app incluye un **proxy** (`Reportes/SsrsProxy.cs`, con YARP) que publica SSRS en `/ReportServer` del **mismo origen**. Esto aporta tres cosas:
  1. El visor se puede incrustar.
  2. Solo los usuarios autenticados con Identity ven los reportes.
  3. El navegador no pide credenciales de Windows.

---

## 7. Instalación y ejecución desde cero

### 7.1 Requisitos
- Visual Studio 2022 (17.12+) o 2026, con la carga de trabajo **ASP.NET y desarrollo web**.
- **SDK de .NET 9**.
- **SQL Server** (Developer 2022 o 2025) como instancia predeterminada `localhost`. **No sirve LocalDB**, porque SSRS la necesita completa.
- **SQL Server Reporting Services 2022** configurado:
  - URL del servicio web: `http://localhost/ReportServer`
  - URL del portal: `http://localhost/Reports`

### 7.2 Permisos (solo si tu usuario de Windows no es administrador)
En PowerShell **como administrador**:

```powershell
# SQL Server: dar acceso sysadmin a tu usuario
sqlcmd -S localhost -E -C -Q "CREATE LOGIN [EQUIPO\usuario] FROM WINDOWS; ALTER SERVER ROLE sysadmin ADD MEMBER [EQUIPO\usuario];"

# SSRS: Administrador del sistema + Administrador de contenido
powershell -ExecutionPolicy Bypass -File .\SSRS\dar-permisos-ssrs.ps1 -Usuario "EQUIPO\usuario"
```

### 7.3 Base de datos
1. Abre `RecetasApp.slnx` en Visual Studio.
2. En `RecetasApi/appsettings.json`, revisa la cadena de conexión (por defecto `Server=localhost;Database=RecetasDB`).
3. Abre la **Consola del Administrador de paquetes** y ejecuta:
   ```
   Update-Database
   ```
   Además, la aplicación aplica las migraciones y crea roles y usuarios al iniciar.

### 7.4 Publicar los reportes en SSRS
Desde la carpeta `SSRS`, en PowerShell (con tu usuario):
```powershell
powershell -ExecutionPolicy Bypass -File .\publicar-reportes.ps1
```
Crea `/Recetas` y sube los dos reportes. Para comprobarlo, abre `http://localhost/Reports` → carpeta **Recetas**.

### 7.5 Ejecutar
1. En Visual Studio, elige el perfil **http** y presiona **F5**. Se abre `http://localhost:5080`.
2. Inicia sesión con un usuario de prueba.
3. Swagger está en `http://localhost:5080/swagger`. Para probar con cookie: `POST /login` con `useCookies=true`.

---

## 8. Endpoints de la API

| Método | Ruta | Acceso |
|---|---|---|
| POST | `/register`, `/login` | Público |
| GET | `/api/Recetas` (`?tiempoMaximo=`), `/api/Recetas/{id}` (incluye ingredientes y pasos) | Autenticado |
| POST/PUT/DELETE | `/api/Recetas`, `/api/Recetas/{id}` | Administrador |
| GET | `/api/Ingredientes` (`?recetaId=`), `/api/Ingredientes/{id}` | Autenticado |
| POST/PUT/DELETE | `/api/Ingredientes`, `/api/Ingredientes/{id}` | Administrador |
| GET | `/api/PasosPreparacion` (`?recetaId=`), `/api/PasosPreparacion/{id}` | Autenticado |
| POST/PUT/DELETE | `/api/PasosPreparacion`, `/api/PasosPreparacion/{id}` | Administrador |
| GET | `/api/Cuenta/yo` · POST `/api/Cuenta/logout` | Autenticado |
| GET | `/api/Cuenta/usuarios` · POST `/api/Cuenta/asignar-rol` | Administrador |
| GET | `/api/Reportes` | Autenticado |
| * | `/ReportServer/...` (visor de SSRS mediante proxy) | Autenticado |

---

## 9. Decisiones técnicas

| Decisión | Motivo |
|---|---|
| `AddIdentityApiEndpoints` + cookies | Es la forma vista en la Guía 10. La aplicación web y la API comparten origen, así que la cookie se envía sola, sin manejar tokens en JavaScript |
| Política `SoloAdministrador` además de roles | Centraliza la regla "solo el Administrador modifica o elimina" en un solo lugar (`Program.cs`) |
| Roles y usuarios creados con `RoleManager`/`UserManager` al iniciar | Las contraseñas se guardan con hash. Hacerlo con `HasData` generaría un hash distinto en cada migración |
| Tiempo de preparación en minutos (`int`) | Permite filtrar y ordenar por tiempo (Reporte 1) |
| Aplicación web en `wwwroot` (HTML + JS) | Mismo origen que la API: no hace falta configurar CORS y todo se ejecuta con un solo F5 |
| Proxy YARP hacia SSRS | SSRS bloquea el iframe entre orígenes (`X-Frame-Options`). El proxy además protege los reportes con Identity |
| Perfil `http` (puerto 5080) | SSRS trabaja en http. Así se evitan bloqueos de contenido mixto y se usa siempre la misma URL |

---

## 10. Pruebas realizadas

Todas se ejecutaron contra SQL Server y SSRS reales en el equipo de desarrollo.

- **API (32 pruebas):**
  - 401 sin sesión; login correcto e incorrecto.
  - Validaciones 400 (nombre de 2 y 101 caracteres; ingrediente de 51 caracteres o sin nombre; paso de 9 caracteres; receta inexistente).
  - CRUD 201/204; registro con rol Usuario automático.
  - 403 del Usuario en POST, PUT y DELETE de las tres entidades y en la gestión de usuarios.
  - Asignación de rol; el administrador no puede cambiar su propio rol.
  - Borrado en cascada; logout.
- **Reportes:**
  - Reporte 1 con `TiempoMaximo=30` → 2 recetas; con 20 → 1; con 60 → 3.
  - Reporte 2 → 3 recetas con "Paso n de 3".
  - El visor funciona dentro del iframe, incluido el botón *Ver informe*; la exportación a PDF funciona.
- **Interfaz:**
  - Login, registro y salir.
  - Como Administrador: crear, editar y eliminar recetas, ingredientes y pasos, con mensajes de validación.
  - Como Usuario: sin botones de edición y sin menú Usuarios.

---

## 11. Cumplimiento del enunciado

| Requisito | Dónde |
|---|---|
| API .NET 9 con CRUD de Recetas, Ingredientes y Pasos | `Controllers/*` |
| Validaciones (obligatorio, longitudes mínimas y máximas) | `Models/*` |
| Identity con roles Administrador y Usuario | `Program.cs`, `Data/SeedIdentity.cs` |
| Autenticados ven; solo Administrador modifica o elimina | `[Authorize]` + política `SoloAdministrador` |
| EF Core + data seed del enunciado | `Data/AppDbContext.cs`, `Migrations/` |
| Reporte 1 (recetas + ingredientes, filtro por tiempo) | `SSRS/Reporte1_RecetasIngredientes.rdl` |
| Reporte 2 (cantidad de pasos y orden por receta) | `SSRS/Reporte2_PasosPorReceta.rdl` |
| Reportes accesibles desde la aplicación web con el visor de SSRS | pestaña **Reportes**, `Reportes/SsrsProxy.cs` |

La guía para la defensa y el guion del video están en [docs/DEFENSA_Y_VIDEO.md](docs/DEFENSA_Y_VIDEO.md).
