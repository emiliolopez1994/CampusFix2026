$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Output = Join-Path $ProjectRoot 'publish'
$Application = Join-Path $Output 'CampusFix.Api.dll'
$Data = Join-Path $ProjectRoot 'database/datos-campusfix.json'
if (!(Test-Path $Application)) { throw 'Ejecuta Preparar.ps1 primero.' }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $Output
try { & dotnet $Application --import $Data; if ($LASTEXITCODE -ne 0) { throw 'La importación falló; revisa el mensaje anterior.' } }
finally { Pop-Location }
