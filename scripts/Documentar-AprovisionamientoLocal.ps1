param(
    [string]$PrivateDirectory = (Join-Path $env:LOCALAPPDATA 'CampusFix\integral-2026-10-10')
)
$ErrorActionPreference = 'Stop'
$report = Get-Content (Join-Path $PrivateDirectory 'resultado-actual.json') -Raw | ConvertFrom-Json
$original = Get-Content (Join-Path $PrivateDirectory 'inventario-antes.json') -Raw | ConvertFrom-Json
$credentials = Get-Content (Join-Path $PrivateDirectory 'accesos-locales.json') -Raw | ConvertFrom-Json
if ($report.host -notin @('localhost', '127.0.0.1', '::1') -or $report.database -ne 'campusfix') { throw 'El inventario no corresponde a CampusFix local.' }
$areas = @{}
foreach ($config in $report.after.config) { if ($config.kind -eq 'areas') { $areas[$config.id] = $config.nombre } }
function AreaName([string]$Id) { if ($Id -and $areas.ContainsKey($Id)) { $areas[$Id] } else { 'Sin área activa identificada' } }
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Accesos privados de CampusFix local')
$lines.Add('')
$lines.Add('Aplicación: http://localhost:4200. Base: localhost:5432/campusfix. Mantener este archivo privado y fuera de Git.')
$lines.Add('Las cuentas nuevas son identidades ficticias; su procedencia sintética se conserva internamente. Sus claves son exclusivas del entorno local.')
foreach ($group in ($credentials | Sort-Object role, areaId, nombre | Group-Object role)) {
    $lines.Add(''); $lines.Add('## ' + $group.Name); $lines.Add('')
    $lines.Add('| Área de pertenencia | Nombre | Correo de acceso | Contraseña inicial |')
    $lines.Add('|---|---|---|---|')
    foreach ($account in $group.Group) { $lines.Add('| ' + (AreaName $account.areaId) + ' | ' + $account.nombre + ' | ' + $account.id + ' | ' + $account.password + ' |') }
}
$lines.Add(''); $lines.Add('## Cuentas existentes conservadas'); $lines.Add('')
$lines.Add('Usar la contraseña actual. No se restablecieron las contraseñas existentes. La verificación manual de estas sesiones queda a cargo del usuario.')
$lines.Add(''); $lines.Add('| Rol | Nombre | Correo | Área de pertenencia |'); $lines.Add('|---|---|---|---|')
foreach ($account in ($original.users | Sort-Object role, nombre)) { $lines.Add('| ' + $account.role + ' | ' + $account.nombre + ' | ' + $account.id + ' | ' + (AreaName $account.areaId) + ' |') }
$lines.Add(''); $lines.Add('## Responsabilidades operativas'); $lines.Add('')
$lines.Add('El área de pertenencia del solicitante puede ser diferente al área que atiende su solicitud. El ámbito administrativo procede de los administradores del área, y el técnico de su pertenencia operativa explícita.')
$lines.Add(''); $lines.Add('| Área de atención | Administradores | Responsable | Solicitudes iniciales |'); $lines.Add('|---|---|---|---|')
foreach ($area in ($report.areas | Sort-Object area)) { $lines.Add('| ' + $area.area + ' | ' + ($area.administratorIds -join ', ') + ' | ' + $area.responsibleId + ' | ' + ($area.ticketIds -join ', ') + ' |') }
[IO.File]::WriteAllLines((Join-Path $PrivateDirectory 'ACCESOS_LOCALES.md'), $lines, [Text.UTF8Encoding]::new($false))

$inventory = [System.Collections.Generic.List[string]]::new()
$inventory.Add('# Inventario institucional local — 10-10-2026'); $inventory.Add('')
$inventory.Add('Base: localhost:5432/campusfix. 8 áreas activas existentes conservadas; 10 usuarios existentes y 52 creados; 59 recursos existentes y 57 creados. Se mantienen 27 tickets anteriores, 40 solicitudes iniciales y una solicitud adicional resuelta durante el recorrido funcional (ID 68).')
$inventory.Add(''); $inventory.Add('## Configuración por área'); $inventory.Add('')
$inventory.Add('| Área | Administradores | Responsable | Equipo | Servicios | Categorías | Calendario | Metas | Tickets iniciales |')
$inventory.Add('|---|---|---|---|---|---|---|---|---|')
function ResourceName([string]$Id) { ($report.after.config | Where-Object id -eq $Id | Select-Object -First 1).nombre }
foreach ($area in ($report.areas | Sort-Object area)) {
    $services = @($area.serviceIds | ForEach-Object { ResourceName $_ }) -join '; '
    $categories = @($area.categoryIds | ForEach-Object { ResourceName $_ }) -join '; '
    $goals = @($report.after.config | Where-Object { $_.kind -eq 'metas' -and $_.responsibleAreaId -eq $area.areaId }).Count
    $inventory.Add('| ' + $area.area + ' | ' + ($area.administratorIds -join ', ') + ' | ' + $area.responsibleId + ' | ' + (ResourceName $area.teamId) + ' | ' + $services + ' | ' + $categories + ' | ' + (ResourceName $area.calendarId) + ' | ' + $goals + ' | ' + ($area.ticketIds -join ', ') + ' |')
}
$inventory.Add(''); $inventory.Add('## Recursos creados y conservados'); $inventory.Add('')
$inventory.Add('Conservado significa que el ID ya existía; algunos recursos recibieron relaciones de ámbito explícitas. No se eliminaron recursos históricos inactivos.')
$inventory.Add(''); $inventory.Add('| Tipo | Nombre | ID | Procedencia |'); $inventory.Add('|---|---|---|---|')
$originalIds = @($original.config | ForEach-Object id)
foreach ($config in ($report.after.config | Sort-Object kind, nombre)) {
    $origin = if ($config.id -in $originalIds) { 'Conservado' } else { 'Creado' }
    $inventory.Add('| ' + $config.kind + ' | ' + $config.nombre + ' | ' + $config.id + ' | ' + $origin + ' |')
}
$inventory.Add(''); $inventory.Add('## Notificaciones y accesos'); $inventory.Add('')
$inventory.Add('Cada área recibió cinco notificaciones iniciales de creación para su administrador y cinco para sus solicitantes: 80 eventos iniciales. Las notificaciones posteriores del flujo y vencimientos son independientes. La carga repetida no duplica eventos ni usuarios. El documento privado ACCESOS_LOCALES.md contiene las 52 claves nuevas agrupadas por rol y área; las 10 cuentas anteriores mantienen sus claves.')
$inventory.Add(''); $inventory.Add('Los inventarios JSON completos, los respaldos pg_dump y el registro del recorrido se conservan en %LOCALAPPDATA%\CampusFix\integral-2026-10-10. No contienen cambios pendientes para producción: ambas migraciones fueron aplicadas exclusivamente a PostgreSQL local.')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot '..\docs\INVENTARIO_LOCAL_2026-10-10.md'), $inventory, [Text.UTF8Encoding]::new($false))
Write-Output 'Documentación de inventario y accesos generada; las contraseñas permanecen en el directorio privado.'
