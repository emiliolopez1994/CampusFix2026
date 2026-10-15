param([switch]$SeedExamples,[string]$PgBin='C:\Program Files\PostgreSQL\18\bin')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$private=Join-Path $env:LOCALAPPDATA 'CampusFix\continuity'
$secretPath=Join-Path $env:APPDATA 'Microsoft/UserSecrets/58096834-e9e3-43af-96f7-3c1422ca4374/secrets.json'
$secrets=Get-Content -LiteralPath $secretPath -Raw|ConvertFrom-Json -AsHashtable
$connection=[System.Data.Common.DbConnectionStringBuilder]::new();$connection.set_ConnectionString($secrets['ConnectionStrings:CampusFix'])
if($connection['Host'] -notin @('localhost','127.0.0.1') -or $connection['Database'] -ne 'campusfix'){throw 'Solo se admite CampusFix PostgreSQL local.'}
$port=if($connection.ContainsKey('Port')){$connection['Port']}else{5432}
$username=if($connection.ContainsKey('Username')){$connection['Username']}else{$connection['User ID']}
$oldPassword=$env:PGPASSWORD;$oldKey=$env:Continuity__ApiKey;$oldConnection=$env:ConnectionStrings__Continuity
try {
 $env:PGPASSWORD=$connection['Password']
 $psql=Join-Path $PgBin 'psql.exe';$pgDump=Join-Path $PgBin 'pg_dump.exe'
 $pgArgs=@('-h','127.0.0.1','-p',"$port",'-U',"$username",'-w','-X','-v','ON_ERROR_STOP=1','-t','-A')
 $hostAddress=& $psql @pgArgs -d campusfix -c 'SELECT host(inet_server_addr())';if($LASTEXITCODE -ne 0 -or $hostAddress.Trim() -notin @('127.0.0.1','::1')){throw 'No se confirmó el servidor local.'}
 New-Item -ItemType Directory -Force -Path $private|Out-Null
 & icacls $private /inheritance:r /grant:r "$($env:USERNAME):(OI)(CI)F" 'SYSTEM:(OI)(CI)F'|Out-Null
 if($LASTEXITCODE -ne 0){throw 'No se pudo proteger el directorio privado.'}
 $stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
 & $pgDump -h 127.0.0.1 -p "$port" -U "$username" -w -d campusfix -Fc -f (Join-Path $private "campusfix-antes-continuity-$stamp.dump")
 if($LASTEXITCODE -ne 0){throw 'No se completó el respaldo principal.'}
 $exists=& $psql @pgArgs -d postgres -c "SELECT 1 FROM pg_database WHERE datname='campusfix_continuity'"
 if($exists -eq '1'){
  & $pgDump -h 127.0.0.1 -p "$port" -U "$username" -w -d campusfix_continuity -Fc -f (Join-Path $private "continuity-antes-$stamp.dump")
  if($LASTEXITCODE -ne 0){throw 'No se completó el respaldo de continuidad.'}
 }else{& $psql @pgArgs -d postgres -c 'CREATE DATABASE campusfix_continuity';if($LASTEXITCODE -ne 0){throw 'No se pudo crear la base independiente.'}}
 $connection['Database']='campusfix_continuity'
 $secrets['ConnectionStrings:Continuity']=$connection.get_ConnectionString()
 if(!$secrets['Continuity:ApiKey']){$secrets['Continuity:ApiKey']=[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))}
 Copy-Item -LiteralPath $secretPath -Destination (Join-Path $private "secrets-antes-$stamp.json")
 $secrets|ConvertTo-Json -Depth 20|Set-Content -LiteralPath $secretPath -Encoding utf8
 $env:Continuity__ApiKey=$secrets['Continuity:ApiKey'];$env:ConnectionStrings__Continuity=$secrets['ConnectionStrings:Continuity']
 $project=Join-Path $projectRoot 'services/CampusFix.Continuity/CampusFix.Continuity.csproj'
 $dll=Join-Path $projectRoot 'services/CampusFix.Continuity/bin/Debug/net10.0/CampusFix.Continuity.dll'
 & dotnet build $project --nologo --no-restore;if($LASTEXITCODE -ne 0){throw 'No se pudo compilar continuidad.'}
 & dotnet $dll --migrate;if($LASTEXITCODE -ne 0){throw 'No se pudo inicializar la base.'}
 if($SeedExamples){
  $sql=@'
SELECT coalesce(json_agg(json_build_object('id',a."Id",'name',a."Data"->>'nombre','ticketId',
 (SELECT t."Id" FROM "Documentos" t WHERE t."Kind"='tickets' AND t."Data"->>'responsibleAreaId'=a."Id"
 AND coalesce((t."Data"->>'eliminado')::boolean,false)=false AND (t."Data"->>'estado')::int<>4
 AND coalesce(t."Data"->>'syntheticOrigin','')<>'' ORDER BY t."Id" LIMIT 1)) ORDER BY a."Id"),'[]')::text
FROM "Documentos" a WHERE a."Kind"='config' AND a."Data"->>'kind'='areas'
AND (a."Data"->>'activo')::boolean AND NOT coalesce((a."Data"->>'eliminado')::boolean,false);
'@
  $seed=& $psql @pgArgs -d campusfix -c $sql;if($LASTEXITCODE -ne 0){throw 'No se pudieron leer las áreas locales.'}
  $file=Join-Path $private 'areas-ejemplos.json';$seed|Set-Content -LiteralPath $file -Encoding utf8
  & dotnet $dll --SeedFile $file --seed-local;if($LASTEXITCODE -ne 0){throw 'No se pudieron cargar los ejemplos.'}
 }
 Write-Host "Continuidad preparada en PostgreSQL local independiente. Respaldos: $private"
}finally{$env:PGPASSWORD=$oldPassword;$env:Continuity__ApiKey=$oldKey;$env:ConnectionStrings__Continuity=$oldConnection}
