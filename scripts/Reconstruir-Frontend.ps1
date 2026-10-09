$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Frontend = Join-Path $ProjectRoot 'frontend/campusfix-web'
Push-Location $Frontend
try { & npm ci; if ($LASTEXITCODE -ne 0) { throw 'Falló npm ci.' }; & npm run build; if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación Angular.' } }
finally { Pop-Location }
$WebRoot = Join-Path $ProjectRoot 'backend/CampusFix.Api/wwwroot'
if (Test-Path $WebRoot) { Remove-Item $WebRoot -Recurse -Force }
Copy-Item (Join-Path $Frontend 'dist/campusfix-web/browser') $WebRoot -Recurse
Write-Host 'Angular actualizado. Ejecuta Preparar.ps1 para publicar la nueva compilación local.'
