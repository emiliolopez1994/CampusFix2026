$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Project = Join-Path $ProjectRoot 'backend/CampusFix.Api/CampusFix.Api.csproj'
$Output = Join-Path $ProjectRoot 'publish'
& dotnet publish $Project -c Release -o $Output
if ($LASTEXITCODE -ne 0) { throw 'La compilación no terminó correctamente.' }
Write-Host 'Compilación lista. Importa los datos una sola vez con Importar-Datos.ps1.'
