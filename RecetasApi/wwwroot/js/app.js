// =====================================================================
// Recetario - aplicación web que consume la API (misma aplicación).
// La sesión se maneja con la cookie de ASP.NET Core Identity.
// =====================================================================

const estado = {
    usuario: null,         // { email, roles }
    esAdmin: false,
    recetas: [],
    recetaActual: null,    // receta seleccionada con ingredientes y pasos
    reportes: [],
    reporteActual: null
};

const $ = (id) => document.getElementById(id);
const modal = (id) => bootstrap.Modal.getOrCreateInstance($(id));

// Escapa texto antes de insertarlo como HTML (evita inyección de código).
function esc(valor) {
    return String(valor ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

function aviso(texto, tipo = "success") {
    const toast = $("toast");
    toast.className = `toast align-items-center border-0 text-bg-${tipo}`;
    $("toastTexto").textContent = texto;
    bootstrap.Toast.getOrCreateInstance(toast, { delay: 3500 }).show();
}

// ---------------------------------------------------------------------
// Llamadas a la API
// ---------------------------------------------------------------------
class ErrorApi extends Error {
    constructor(status, mensajes) {
        super(mensajes.join(" "));
        this.status = status;
        this.mensajes = mensajes;
    }
}

async function api(metodo, url, cuerpo) {
    const opciones = { method: metodo, headers: {} };
    if (cuerpo !== undefined) {
        opciones.headers["Content-Type"] = "application/json";
        opciones.body = JSON.stringify(cuerpo);
    }

    const resp = await fetch(url, opciones);

    if (resp.status === 401 && estado.usuario) {
        mostrarAcceso();
        throw new ErrorApi(401, ["Su sesión expiró. Inicie sesión nuevamente."]);
    }
    if (resp.status === 403) {
        throw new ErrorApi(403, ["No tiene permisos para realizar esta acción (solo Administrador)."]);
    }
    if (!resp.ok) {
        throw new ErrorApi(resp.status, await leerErrores(resp));
    }

    const texto = await resp.text();
    if (!texto) return null;
    try { return JSON.parse(texto); } catch { return texto; }
}

// Convierte la respuesta de error (ProblemDetails, texto) en una lista de mensajes.
async function leerErrores(resp) {
    const texto = await resp.text();
    try {
        const datos = JSON.parse(texto);
        if (datos.errors) return Object.values(datos.errors).flat();
        if (datos.detail) return [datos.detail];
        if (datos.title) return [datos.title];
        if (typeof datos === "string") return [datos];
    } catch { /* no es JSON */ }
    return [texto || `Error ${resp.status}`];
}

// Muestra una lista de mensajes de error dentro de un contenedor.
function listaErrores(caja, mensajes) {
    caja.innerHTML = mensajes.length === 1
        ? esc(mensajes[0])
        : `<ul class="mb-0 ps-3">${mensajes.map(m => `<li>${esc(m)}</li>`).join("")}</ul>`;
    caja.classList.remove("d-none");
}

function mostrarErrores(form, error) {
    listaErrores(form.querySelector(".errores"), error.mensajes ?? [error.message]);
}

function limpiarErrores(form) {
    const caja = form.querySelector(".errores");
    if (caja) { caja.classList.add("d-none"); caja.innerHTML = ""; }
}

// ---------------------------------------------------------------------
// Sesión: login, registro, logout
// ---------------------------------------------------------------------
async function iniciar() {
    try {
        const resp = await fetch("/api/Cuenta/yo");
        if (!resp.ok) return mostrarAcceso();
        estado.usuario = await resp.json();
        await mostrarApp();
    } catch {
        mostrarAcceso();
    }
}

function mostrarAcceso() {
    estado.usuario = null;
    $("vistaApp").classList.add("d-none");
    $("vistaAcceso").classList.remove("d-none");
}

async function mostrarApp() {
    estado.esAdmin = estado.usuario.roles.includes("Administrador");
    document.body.classList.toggle("es-admin", estado.esAdmin);

    $("lblEmail").textContent = estado.usuario.email;
    const rol = estado.usuario.roles.join(", ") || "Sin rol";
    $("lblRol").textContent = rol;
    $("lblRol").className = `badge ${estado.esAdmin ? "text-bg-warning" : "text-bg-info"}`;

    $("vistaAcceso").classList.add("d-none");
    $("vistaApp").classList.remove("d-none");
    cambiarVista("recetas");
    await cargarRecetas();
}

$("formLogin").addEventListener("submit", async (e) => {
    e.preventDefault();
    const error = $("loginError");
    error.classList.add("d-none");

    const resp = await fetch("/login?useCookies=true&useSessionCookies=true", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: $("loginEmail").value.trim(), password: $("loginPassword").value })
    });

    if (!resp.ok) {
        error.textContent = "Correo o contraseña incorrectos.";
        error.classList.remove("d-none");
        return;
    }
    $("loginPassword").value = "";
    await iniciar();
});

$("formRegistro").addEventListener("submit", async (e) => {
    e.preventDefault();
    const error = $("regError");
    error.classList.add("d-none");

    const email = $("regEmail").value.trim();
    const password = $("regPassword").value;
    const resp = await fetch("/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password })
    });

    if (!resp.ok) {
        listaErrores(error, await leerErrores(resp));
        return;
    }

    // Registro correcto: inicia sesión automáticamente
    await fetch("/login?useCookies=true&useSessionCookies=true", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password })
    });
    $("regPassword").value = "";
    aviso("Cuenta creada con el rol Usuario.");
    await iniciar();
});

$("btnSalir").addEventListener("click", async () => {
    await fetch("/api/Cuenta/logout", { method: "POST" });
    $("visorReporte").src = "about:blank";
    mostrarAcceso();
});

// ---------------------------------------------------------------------
// Navegación
// ---------------------------------------------------------------------
function cambiarVista(vista) {
    document.querySelectorAll(".vista").forEach(v => v.classList.add("d-none"));
    $("vista" + vista.charAt(0).toUpperCase() + vista.slice(1)).classList.remove("d-none");
    document.querySelectorAll("[data-vista]").forEach(a => a.classList.toggle("active", a.dataset.vista === vista));
}

document.querySelectorAll("[data-vista]").forEach(enlace => {
    enlace.addEventListener("click", async (e) => {
        e.preventDefault();
        const vista = enlace.dataset.vista;
        cambiarVista(vista);
        if (vista === "reportes") await cargarReportes();
        if (vista === "usuarios") await cargarUsuarios();
    });
});

// ---------------------------------------------------------------------
// Recetas
// ---------------------------------------------------------------------
async function cargarRecetas() {
    const tiempo = $("selTiempoMax").value;
    estado.recetas = await api("GET", "/api/Recetas" + (tiempo ? `?tiempoMaximo=${tiempo}` : ""));
    pintarRecetas();
}

function pintarRecetas() {
    const filtro = $("txtBuscar").value.trim().toLowerCase();
    const recetas = estado.recetas.filter(r => r.nombre.toLowerCase().includes(filtro));
    const lista = $("listaRecetas");

    if (recetas.length === 0) {
        lista.innerHTML = `<div class="text-muted text-center p-4">No se encontraron recetas.</div>`;
        return;
    }

    lista.innerHTML = recetas.map(r => `
        <a class="list-group-item list-group-item-action ${estado.recetaActual?.id === r.id ? "active" : ""}" data-id="${r.id}">
            <div class="d-flex justify-content-between">
                <span class="fw-semibold">${esc(r.nombre)}</span>
                <span class="badge text-bg-light border"><i class="bi bi-clock"></i> ${r.tiempoPreparacion} min</span>
            </div>
            <div class="small text-muted text-truncate">${esc(r.descripcion)}</div>
        </a>`).join("");

    lista.querySelectorAll("[data-id]").forEach(item =>
        item.addEventListener("click", () => seleccionarReceta(Number(item.dataset.id))));
}

$("txtBuscar").addEventListener("input", pintarRecetas);
$("selTiempoMax").addEventListener("change", cargarRecetas);

async function seleccionarReceta(id) {
    try {
        estado.recetaActual = await api("GET", `/api/Recetas/${id}`);
        pintarDetalle();
        pintarRecetas();
    } catch (e) {
        aviso(e.message, "danger");
    }
}

function pintarDetalle() {
    const r = estado.recetaActual;
    if (!r) {
        $("detalleReceta").classList.add("d-none");
        $("detalleVacio").classList.remove("d-none");
        return;
    }
    $("detalleVacio").classList.add("d-none");
    $("detalleReceta").classList.remove("d-none");

    $("detNombre").textContent = r.nombre;
    $("detDescripcion").textContent = r.descripcion || "Sin descripción.";
    $("detTiempo").textContent = r.tiempoPreparacion;
    $("detNumIng").textContent = r.ingredientes.length;
    $("detNumPasos").textContent = r.pasos.length;

    const botones = (tipo, id) => `
        <td class="solo-admin text-end text-nowrap">
            <button class="btn btn-sm btn-link p-1" data-editar="${tipo}" data-id="${id}" title="Editar"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-sm btn-link text-danger p-1" data-eliminar="${tipo}" data-id="${id}" title="Eliminar"><i class="bi bi-trash"></i></button>
        </td>`;

    $("tablaIngredientes").innerHTML = r.ingredientes.length === 0
        ? `<tr><td colspan="4" class="text-muted text-center py-3">Sin ingredientes.</td></tr>`
        : r.ingredientes.map(i => `
            <tr>
                <td>${esc(i.nombre)}</td>
                <td class="text-end">${Number(i.cantidad).toLocaleString("es")}</td>
                <td>${esc(i.unidadMedida)}</td>
                ${botones("ingrediente", i.id)}
            </tr>`).join("");

    $("listaPasos").innerHTML = r.pasos.length === 0
        ? `<li class="list-group-item text-muted text-center py-3">Sin pasos registrados.</li>`
        : r.pasos.map(p => `
            <li class="list-group-item d-flex gap-3 align-items-center">
                <span class="numero-paso">${p.orden}</span>
                <span class="flex-grow-1">${esc(p.descripcion)}</span>
                <table class="solo-admin"><tr>${botones("paso", p.id)}</tr></table>
            </li>`).join("");

    document.querySelectorAll("[data-editar]").forEach(b => b.addEventListener("click", () => {
        if (b.dataset.editar === "ingrediente") abrirIngrediente(Number(b.dataset.id));
        else abrirPaso(Number(b.dataset.id));
    }));
    document.querySelectorAll("[data-eliminar]").forEach(b => b.addEventListener("click", () => {
        if (b.dataset.eliminar === "ingrediente") eliminarIngrediente(Number(b.dataset.id));
        else eliminarPaso(Number(b.dataset.id));
    }));
}

// ----- Crear / editar receta -----
function abrirReceta(receta) {
    const form = $("formReceta");
    limpiarErrores(form);
    $("tituloModalReceta").textContent = receta ? "Editar receta" : "Nueva receta";
    $("recId").value = receta?.id ?? "";
    $("recNombre").value = receta?.nombre ?? "";
    $("recDescripcion").value = receta?.descripcion ?? "";
    $("recTiempo").value = receta?.tiempoPreparacion ?? "";
    modal("modalReceta").show();
}

$("btnNuevaReceta").addEventListener("click", () => abrirReceta(null));
$("btnEditarReceta").addEventListener("click", () => abrirReceta(estado.recetaActual));

$("formReceta").addEventListener("submit", async (e) => {
    e.preventDefault();
    const form = e.target;
    limpiarErrores(form);
    const id = $("recId").value;
    const datos = {
        nombre: $("recNombre").value.trim(),
        descripcion: $("recDescripcion").value.trim() || null,
        tiempoPreparacion: Number($("recTiempo").value) || 0
    };

    try {
        let recetaId = Number(id);
        if (id) {
            await api("PUT", `/api/Recetas/${id}`, { id: recetaId, ...datos });
            aviso("Receta actualizada.");
        } else {
            const nueva = await api("POST", "/api/Recetas", datos);
            recetaId = nueva.id;
            aviso("Receta creada.");
        }
        modal("modalReceta").hide();
        await cargarRecetas();
        await seleccionarReceta(recetaId);
    } catch (error) {
        mostrarErrores(form, error);
    }
});

$("btnEliminarReceta").addEventListener("click", () => {
    const r = estado.recetaActual;
    confirmar(`¿Eliminar la receta "${r.nombre}" con todos sus ingredientes y pasos?`, async () => {
        await api("DELETE", `/api/Recetas/${r.id}`);
        estado.recetaActual = null;
        pintarDetalle();
        await cargarRecetas();
        aviso("Receta eliminada.");
    });
});

// ----- Ingredientes -----
function abrirIngrediente(id) {
    const ingrediente = estado.recetaActual.ingredientes.find(i => i.id === id);
    const form = $("formIngrediente");
    limpiarErrores(form);
    $("tituloModalIngrediente").textContent = ingrediente ? "Editar ingrediente" : "Nuevo ingrediente";
    $("ingId").value = ingrediente?.id ?? "";
    $("ingNombre").value = ingrediente?.nombre ?? "";
    $("ingCantidad").value = ingrediente?.cantidad ?? "";
    $("ingUnidad").value = ingrediente?.unidadMedida ?? "";
    modal("modalIngrediente").show();
}

$("btnNuevoIngrediente").addEventListener("click", () => abrirIngrediente(null));

$("formIngrediente").addEventListener("submit", async (e) => {
    e.preventDefault();
    const form = e.target;
    limpiarErrores(form);
    const id = $("ingId").value;
    const datos = {
        nombre: $("ingNombre").value.trim(),
        cantidad: Number($("ingCantidad").value) || 0,
        unidadMedida: $("ingUnidad").value.trim(),
        recetaId: estado.recetaActual.id
    };

    try {
        if (id) await api("PUT", `/api/Ingredientes/${id}`, { id: Number(id), ...datos });
        else await api("POST", "/api/Ingredientes", datos);
        modal("modalIngrediente").hide();
        aviso(id ? "Ingrediente actualizado." : "Ingrediente agregado.");
        await seleccionarReceta(estado.recetaActual.id);
    } catch (error) {
        mostrarErrores(form, error);
    }
});

function eliminarIngrediente(id) {
    const ingrediente = estado.recetaActual.ingredientes.find(i => i.id === id);
    confirmar(`¿Eliminar el ingrediente "${ingrediente.nombre}"?`, async () => {
        await api("DELETE", `/api/Ingredientes/${id}`);
        await seleccionarReceta(estado.recetaActual.id);
        aviso("Ingrediente eliminado.");
    });
}

// ----- Pasos de preparación -----
function abrirPaso(id) {
    const paso = estado.recetaActual.pasos.find(p => p.id === id);
    const form = $("formPaso");
    limpiarErrores(form);
    $("tituloModalPaso").textContent = paso ? "Editar paso" : "Nuevo paso";
    $("pasoId").value = paso?.id ?? "";
    $("pasoOrden").value = paso?.orden ?? (estado.recetaActual.pasos.length + 1);
    $("pasoDescripcion").value = paso?.descripcion ?? "";
    modal("modalPaso").show();
}

$("btnNuevoPaso").addEventListener("click", () => abrirPaso(null));

$("formPaso").addEventListener("submit", async (e) => {
    e.preventDefault();
    const form = e.target;
    limpiarErrores(form);
    const id = $("pasoId").value;
    const datos = {
        orden: Number($("pasoOrden").value) || 0,
        descripcion: $("pasoDescripcion").value.trim(),
        recetaId: estado.recetaActual.id
    };

    try {
        if (id) await api("PUT", `/api/PasosPreparacion/${id}`, { id: Number(id), ...datos });
        else await api("POST", "/api/PasosPreparacion", datos);
        modal("modalPaso").hide();
        aviso(id ? "Paso actualizado." : "Paso agregado.");
        await seleccionarReceta(estado.recetaActual.id);
    } catch (error) {
        mostrarErrores(form, error);
    }
});

function eliminarPaso(id) {
    const paso = estado.recetaActual.pasos.find(p => p.id === id);
    confirmar(`¿Eliminar el paso ${paso.orden}?`, async () => {
        await api("DELETE", `/api/PasosPreparacion/${id}`);
        await seleccionarReceta(estado.recetaActual.id);
        aviso("Paso eliminado.");
    });
}

// ----- Confirmación de eliminación -----
let accionConfirmada = null;

function confirmar(texto, accion) {
    $("textoConfirmar").textContent = texto;
    accionConfirmada = accion;
    modal("modalConfirmar").show();
}

$("btnConfirmar").addEventListener("click", async () => {
    modal("modalConfirmar").hide();
    try {
        await accionConfirmada?.();
    } catch (e) {
        aviso(e.message, "danger");
    }
});

// ---------------------------------------------------------------------
// Reportes (visor de SSRS)
// ---------------------------------------------------------------------
async function cargarReportes() {
    if (estado.reportes.length === 0) {
        estado.reportes = await api("GET", "/api/Reportes");
        $("tabsReportes").innerHTML = estado.reportes.map(r => `
            <li class="nav-item"><button class="nav-link" data-reporte="${r.id}"><i class="bi bi-file-earmark-bar-graph me-1"></i>${esc(r.nombre)}</button></li>`).join("");
        document.querySelectorAll("[data-reporte]").forEach(b =>
            b.addEventListener("click", () => mostrarReporte(b.dataset.reporte)));
    }
    mostrarReporte(estado.reporteActual?.id ?? estado.reportes[0].id);
}

function mostrarReporte(id) {
    const reporte = estado.reportes.find(r => r.id === id);
    estado.reporteActual = reporte;
    document.querySelectorAll("[data-reporte]").forEach(b => b.classList.toggle("active", b.dataset.reporte === id));
    $("descReporte").textContent = reporte.descripcion;
    $("filtroReporte").classList.toggle("d-none", !reporte.parametro);

    let url = reporte.url;
    if (reporte.parametro) url += `&${reporte.parametro}=${$("selTiempoReporte").value}`;

    $("visorReporte").src = url;
    $("lnkReporte").href = url;
}

$("selTiempoReporte").addEventListener("change", () => mostrarReporte(estado.reporteActual.id));

// ---------------------------------------------------------------------
// Usuarios y roles (solo Administrador)
// ---------------------------------------------------------------------
async function cargarUsuarios() {
    try {
        const usuarios = await api("GET", "/api/Cuenta/usuarios");
        $("tablaUsuarios").innerHTML = usuarios.map(u => {
            const esYo = u.email === estado.usuario.email;
            const rolActual = u.roles[0] ?? "";
            return `
            <tr>
                <td>${esc(u.email)} ${esYo ? '<span class="badge text-bg-secondary">Usted</span>' : ""}</td>
                <td>${u.roles.length ? u.roles.map(r => `<span class="badge ${r === "Administrador" ? "text-bg-warning" : "text-bg-info"}">${esc(r)}</span>`).join(" ") : '<span class="text-muted">Sin rol</span>'}</td>
                <td>
                    <div class="input-group input-group-sm">
                        <select class="form-select" data-email="${esc(u.email)}" ${esYo ? "disabled" : ""}>
                            <option value="Usuario" ${rolActual === "Usuario" ? "selected" : ""}>Usuario</option>
                            <option value="Administrador" ${rolActual === "Administrador" ? "selected" : ""}>Administrador</option>
                        </select>
                        <button class="btn btn-outline-primary" data-guardar-rol="${esc(u.email)}" ${esYo ? "disabled" : ""}>Guardar</button>
                    </div>
                </td>
            </tr>`;
        }).join("");

        document.querySelectorAll("[data-guardar-rol]").forEach(b => b.addEventListener("click", async () => {
            const email = b.dataset.guardarRol;
            const rol = document.querySelector(`select[data-email="${CSS.escape(email)}"]`).value;
            try {
                await api("POST", "/api/Cuenta/asignar-rol", { email, rol });
                aviso(`Rol ${rol} asignado a ${email}.`);
                await cargarUsuarios();
            } catch (e) {
                aviso(e.message, "danger");
            }
        }));
    } catch (e) {
        aviso(e.message, "danger");
    }
}

iniciar();
