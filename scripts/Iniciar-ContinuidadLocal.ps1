$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$secretPath=Join-Path $env:APPDATA 'Microsoft/UserSecrets/58096834-e9e3-43af-96f7-3c1422ca4374/secrets.json'
$secrets=Get-Content -LiteralPath $secretPath -Raw|ConvertFrom-Json -AsHashtable
if(!$secrets['Continuity:ApiKey'] -or !$secrets['ConnectionStrings:Continuity']){throw 'Ejecuta Preparar-ContinuidadLocal.ps1 primero.'}
$oldKey=$env:Continuity__ApiKey;$oldConnection=$env:ConnectionStrings__Continuity
try{
 $env:Continuity__ApiKey=$secrets['Continuity:ApiKey'];$env:ConnectionStrings__Continuity=$secrets['ConnectionStrings:Continuity']
 & dotnet (Join-Path $projectRoot 'services/CampusFix.Continuity/bin/Debug/net10.0/CampusFix.Continuity.dll')
}finally{$env:Continuity__ApiKey=$oldKey;$env:ConnectionStrings__Continuity=$oldConnection}
