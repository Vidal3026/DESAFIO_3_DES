# Publica los reportes .rdl de esta carpeta en SQL Server Reporting Services.
# Crea la carpeta /Recetas (si no existe) y sube (o reemplaza) cada reporte.
#
# Uso (desde PowerShell, en la carpeta SSRS):
#   powershell -ExecutionPolicy Bypass -File .\publicar-reportes.ps1
#   powershell -ExecutionPolicy Bypass -File .\publicar-reportes.ps1 -ServidorUrl "http://localhost/ReportServer"

param(
    [string]$ServidorUrl = "http://localhost/ReportServer",
    [string]$Carpeta = "Recetas"
)

$ErrorActionPreference = "Stop"

$rs = New-WebServiceProxy -Uri "$ServidorUrl/ReportService2010.asmx?wsdl" -UseDefaultCredential

# 1. Carpeta /Recetas
$existe = $rs.ListChildren("/", $false) | Where-Object { $_.Path -eq "/$Carpeta" }
if (-not $existe) {
    $rs.CreateFolder($Carpeta, "/", $null) | Out-Null
    Write-Host "Carpeta /$Carpeta creada."
}

# 2. Reportes
Get-ChildItem -Path $PSScriptRoot -Filter *.rdl | ForEach-Object {
    $nombre = $_.BaseName
    $contenido = [System.IO.File]::ReadAllBytes($_.FullName)
    $advertencias = $null
    $rs.CreateCatalogItem("Report", $nombre, "/$Carpeta", $true, $contenido, $null, [ref]$advertencias) | Out-Null
    Write-Host "Reporte publicado: /$Carpeta/$nombre"
}

Write-Host "Listo. Abra http://localhost/Reports para verlos en el portal."
