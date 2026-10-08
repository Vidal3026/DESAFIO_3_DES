# Da permisos en SSRS a un usuario de Windows que no es administrador del equipo.
# Debe ejecutarse en PowerShell abierto "Como administrador".
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File .\dar-permisos-ssrs.ps1 -Usuario "EQUIPO\usuario"

param(
    [Parameter(Mandatory = $true)][string]$Usuario,
    [string]$ServidorUrl = "http://localhost/ReportServer"
)

$ErrorActionPreference = "Stop"

$rs = New-WebServiceProxy -Uri "$ServidorUrl/ReportService2010.asmx?wsdl" -UseDefaultCredential
$ns = $rs.GetType().Namespace

# Permiso de sitio: Administrador del sistema
$p = New-Object "$ns.Policy"; $p.GroupUserName = $Usuario
$r = New-Object "$ns.Role"; $r.Name = "System Administrator"; $p.Roles = @($r)
$rs.SetSystemPolicies(@($rs.GetSystemPolicies() | Where-Object { $_.GroupUserName -ne $Usuario }) + $p)

# Permiso en la carpeta Inicio: Administrador de contenido
$heredado = $true
$actuales = $rs.GetPolicies("/", [ref]$heredado)
$p2 = New-Object "$ns.Policy"; $p2.GroupUserName = $Usuario
$r2 = New-Object "$ns.Role"; $r2.Name = "Content Manager"; $p2.Roles = @($r2)
$rs.SetPolicies("/", @($actuales | Where-Object { $_.GroupUserName -ne $Usuario }) + $p2)

Write-Host "Permisos asignados a $Usuario."
