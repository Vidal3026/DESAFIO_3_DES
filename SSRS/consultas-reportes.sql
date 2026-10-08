-- Consultas que usan los reportes de SSRS (base de datos RecetasDB).
-- Son las mismas que están dentro de cada archivo .rdl (DataSet).

-- =====================================================================
-- Reporte 1: Recetas y sus ingredientes, filtradas por tiempo de preparación.
-- @TiempoMaximo es el parámetro del reporte (minutos). LEFT JOIN para que
-- también aparezcan recetas sin ingredientes.
-- =====================================================================
DECLARE @TiempoMaximo int = 30;

SELECT r.Nombre AS Receta,
       r.TiempoPreparacion AS TiempoMinutos,
       i.Nombre AS Ingrediente,
       i.Cantidad,
       i.UnidadMedida
FROM Recetas r
LEFT JOIN Ingredientes i ON i.RecetaId = r.Id
WHERE r.TiempoPreparacion <= @TiempoMaximo
ORDER BY r.TiempoPreparacion, r.Nombre, i.Nombre;

-- =====================================================================
-- Reporte 2: Pasos de preparación por receta: cuántos pasos tiene cada
-- receta (COUNT ... OVER) y el orden en que se realizan.
-- =====================================================================
SELECT r.Nombre AS Receta,
       COUNT(p.Id) OVER (PARTITION BY r.Id) AS TotalPasos,
       p.Orden,
       p.Descripcion AS Paso
FROM Recetas r
LEFT JOIN PasosPreparacion p ON p.RecetaId = r.Id
ORDER BY r.Nombre, p.Orden;
