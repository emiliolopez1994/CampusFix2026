param([switch]$Build)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'services/CampusFix.Reports/CampusFix.Reports.csproj'
$dll = Join-Path $projectRoot 'services/CampusFix.Reports/bin/Debug/net10.0/CampusFix.Reports.dll'
$secretPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/58096834-e9e3-43af-96f7-3c1422ca4374/secrets.json'
$secrets = @{}
if (Test-Path -LiteralPath $secretPath) {
    $secretObject = Get-Content -LiteralPath $secretPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($property in $secretObject.PSObject.Properties) {
        $secrets[$property.Name] = $property.Value
    }
}
if (!$secrets['Reports:ApiKey']) {
    $keyBytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($keyBytes) } finally { $rng.Dispose() }
    $secrets['Reports:ApiKey'] = [BitConverter]::ToString($keyBytes).Replace('-', '')
    New-Item -ItemType Directory -Force -Path (Split-Path $secretPath) | Out-Null
    $secrets | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $secretPath -Encoding utf8
}
if ($Build -or !(Test-Path $dll)) {
    & dotnet build $project --nologo
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el microservicio.' }
}
$previousKey = $env:Reports__ApiKey
try {
    $env:Reports__ApiKey = $secrets['Reports:ApiKey']
    Write-Host 'Informes locales: http://127.0.0.1:5148/health. Reinicia la API local tras la primera configuración.'
    & dotnet $dll
} finally { $env:Reports__ApiKey = $previousKey }
