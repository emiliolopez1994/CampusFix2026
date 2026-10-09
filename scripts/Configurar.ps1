$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Project = Join-Path $ProjectRoot 'backend/CampusFix.Api/CampusFix.Api.csproj'
Write-Host 'Configura la base PostgreSQL que creaste. No se cambiará la página publicada.'
$ServerName = Read-Host 'Servidor (Enter = localhost)'
if (!$ServerName) { $ServerName = 'localhost' }
$PortNumber = Read-Host 'Puerto (Enter = 5432)'
if (!$PortNumber) { $PortNumber = '5432' }
$DatabaseName = Read-Host 'Base de datos (Enter = campusfix)'
if (!$DatabaseName) { $DatabaseName = 'campusfix' }
$UserName = Read-Host 'Usuario de PostgreSQL (Enter = campusfix)'
if (!$UserName) { $UserName = 'campusfix' }
$SecurePassword = Read-Host 'Contraseña PostgreSQL' -AsSecureString
$PlainPassword = [System.Net.NetworkCredential]::new('', $SecurePassword).Password
function Quote-Connection([string]$Value) { return '"' + $Value.Replace('"','""') + '"' }
$Connection = 'Host=' + (Quote-Connection $ServerName) + ';Port=' + $PortNumber + ';Database=' + (Quote-Connection $DatabaseName) + ';Username=' + (Quote-Connection $UserName) + ';Password=' + (Quote-Connection $PlainPassword)
function Random-Key { $bytes = New-Object byte[] 64; $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($bytes); $rng.Dispose(); return [Convert]::ToBase64String($bytes) }
$Secrets = @{ 'ConnectionStrings:CampusFix'=$Connection; 'Jwt:Key'=(Random-Key); 'Mail:EncryptionKey'=(Random-Key) }
$Secrets | ConvertTo-Json | & dotnet user-secrets set --project $Project
if ($LASTEXITCODE -ne 0) { throw 'No se guardó la configuración.' }
$PlainPassword = $null; $Secrets = $null
Write-Host 'Configuración guardada fuera del código. Continúa con Preparar.ps1.'
