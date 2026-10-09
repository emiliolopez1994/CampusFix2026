$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Output = Join-Path $ProjectRoot 'publish'
$Application = Join-Path $Output 'CampusFix.Api.dll'
if (!(Test-Path $Application)) { throw 'Ejecuta Preparar.ps1 primero.' }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $Output
try { Write-Host 'Abre http://localhost:5137 . Pulsa Ctrl+C para detener.'; & dotnet $Application --urls http://localhost:5137 }
finally { Pop-Location }
