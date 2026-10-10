# CampusFix — implementación integral local (10-10-2026)

## Diagnóstico antes de modificar

Base efectiva confirmada mediante UserSecrets de Development, conexión PostgreSQL y `inet_server_addr`: localhost:5432/campusfix (::1). Respaldo previo: `C:\Users\lio13\AppData\Local\CampusFix\integral-2026-10-10\antes-20261010-112918.dump`. Se conserva además un respaldo por cada ejecución del aprovisionador.

8 áreas activas (no 15): Admisiones, Biblioteca, Bienestar estudiantil, Dirección Académica, Mantenimiento, Registro Académico, Secretaría Académica y Tecnologías de la Información. Inicialmente: 10 usuarios, 59 recursos, 27 tickets, 273 avisos; todas las áreas activas tenían managers vacío. Existían equipo de Bienestar, administrador, Fabián Puma, dos servicios y dos categorías: reutilizados por ID. Se conservan duplicados históricos desactivados y sedes repetidas; no se borraron ni se reactivaron automáticamente. Elementos padre preservados, incluido el de Bienestar, que actualmente depende de Dirección Académica.

QA-30: la condición de clasificación también dependía de tener un administrador activo. Faltaba esa relación; clasificar aparentaba éxito pero la condición seguía incumplida. Ahora informa la causa, valida responsables, guarda el resultado y permite clasificación a administradores del ámbito. Las lecturas ya no derivan silenciosamente snapshots históricos desde un servicio modificado; el backfill se aplica mediante migración explícita.

QA-31: el rol Solicitante guardado tenía panel=false/tablero=true, y el login enviaba siempre a panel; el guard terminaba en cuenta. Ahora el destino es tablero conforme a permisos, salvo flags explícitos mustChange/needsProfile. La pertenencia permanece guardada y el cambio voluntario de contraseña sigue disponible.

## Implementación y pruebas por etapa

- Configuración: área responsable y sharedAreaIds en recursos, categorías derivan el ámbito desde su servicio. Globales se conservan y solo el propietario los edita. Compartidos requieren administrar todas sus áreas para editar. Administrador no puede apropiarse de recursos cambiando el payload ni otorgarse áreas desde Elemento padre/managers.
- Equipos y asignaciones: elegibilidad por operationalAreas o pertenencia a equipo de área explícita; multiárea admitida. Catálogo administrativo filtrado en servidor e interfaz. Etiqueta de solicitudes: Responsable asignado.
- Formulario: Área solicitante preservada; Área de atención filtra servicio/categoría y limpia valores incompatibles. Ayuda general continúa disponible. Backend valida coherencia cuando el consumidor envía attentionAreaId; consumidores anteriores siguen admitidos.
- Configuración por área: selector persistente por usuario, Todas las áreas para propietario, global/compartido identificado, padres preservados, creación preselecciona ámbito.
- Verificación de etapa: backend y Angular compilan; 98 comprobaciones de negocio PostgreSQL más prueba de migración, 34 pruebas de Workspace/Auth de los cambios agrupados.

## Carga local ya ejecutada

Aprovisionador integrado: `--provision-local --local-approved`, requiere Development, host loopback y dirección real del servidor loopback. Sin `--apply` genera inventario y previsualiza sin escribir en BD. Con `--apply` realiza pg_dump, aplica migraciones, bloquea el lote y ejecuta transacción. Marcadores internos estables identifican recursos/tickets y origen sintético. Se usan validaciones de configuración, creación de tickets y eventos del dominio. No se manda correo externo; no se alteran claves existentes. Las credenciales nuevas se guardan fuera del repositorio en un directorio privado.

Migraciones aplicadas localmente: `20261010010000_ServiceRouting` y `20261010120000_ResourceScope`. La segunda obtiene ámbito de equipos legados solo desde referencias explícitas de servicios/categorías; soporta equipos compartidos y no inventa relaciones por nombre.

Resultado: 52 cuentas nuevas, 57 recursos nuevos, 40 tickets nuevos y 80 avisos de creación (40 para solicitantes, 40 para administradores). Se conservan los 27 tickets anteriores. Cada área tiene 5 solicitudes Reportado, 5 solicitantes distintos y 5 avisos iniciales no leídos. La consulta `database/verificar-aprovisionamiento-local.sql` verificó las ocho filas y cero eventos duplicados.

Segunda ejecución: usuarios 62→62, recursos 116→116, tickets 67→67 y avisos 353→353. Las dos ejecuciones completas y los inventarios antes/después están en el directorio privado. API/Angular reiniciados en localhost:5137 y localhost:4200 con la versión actual.

## Cierre de implementación y regresión

QA-30/31/34/35/36/37 implementados. La aceptación manual final queda a cargo del usuario por indicación expresa; no se presenta como una prueba ejecutada por Codex. Las cuentas existentes mantienen sus contraseñas. Se retiró una opción de sesiones automáticas rechazada por revisión de aprobación; no existe endpoint ni comando para eludir autenticación.

| Verificación | Resultado |
|---|---|
| Angular, suite completa | 58/58, nueve archivos |
| Negocio con PostgreSQL aislado | 98/98; transacción revertida al terminar |
| Migraciones de rutas y ámbito | Backfill, idempotencia y reversión conservan historial y miembros |
| API HTTP real y PostgreSQL aislado | 102/102 |
| Edge con cuatro roles y contraseñas de fixture | 13/13, incluye registro, adjunto, asignación, sincronización, solución, valoración, perfil y móvil |
| Instalación local aprovisionada | 8/8: siete administradores nuevos, permisos, catálogo, tres inicios de solicitante y solicitud adicional resuelta |
| SQL local tras reiniciar | Ocho áreas con cinco reportes, cinco solicitantes y cinco avisos iniciales no leídos; cero eventos duplicados |

Se comprobó el flujo de Superusuario y administrador de Bienestar con cuentas aisladas mediante autenticación normal; el acceso a sus cuentas preexistentes en la instalación principal queda para la comprobación manual del usuario. Los 40 reportes iniciales permanecen en Reportado, sin consumirlos durante el recorrido. El ticket adicional 68 terminó Solucionado y conserva internamente su procedencia sintética. API y Angular se reiniciaron con la versión final Integral para verificar la persistencia.

Totales finales de la carga: 62 usuarios, 116 recursos y 68 tickets (27 anteriores + 40 iniciales + uno del recorrido). La última repetición del aprovisionamiento mantuvo 62→62 usuarios, 116→116 recursos, 68→68 tickets y 366→366 avisos. El total de avisos incluye historial, el recorrido y recordatorios; los 80 avisos iniciales del lote se verifican por tipo/evento, sin confundirlos con esos otros avisos. No hay migraciones pendientes en la base local.

Incidencias de herramientas resueltas: la primera ejecución final de Vitest agotó el tiempo al iniciar nueve workers; con `vitest-local.config.mts` (un worker) pasó completa. Una llamada HTTP se lanzó antes de finalizar la compilación del arnés; se repitió al existir el binario y pasó. NuGet emitió NU1900 por no poder consultar el índice remoto de vulnerabilidades; no impidió compilar ni probar y no constituye una validación de vulnerabilidades.

## Entrega y reproducción

- Aplicación local: http://localhost:4200; API: http://localhost:5137.
- Inventario de áreas, responsables, equipos, servicios, categorías, calendarios, metas y los 116 recursos: `docs/INVENTARIO_LOCAL_2026-10-10.md`.
- Accesos organizados por rol y área: `%LOCALAPPDATA%\CampusFix\integral-2026-10-10\ACCESOS_LOCALES.md`. Las 52 claves nuevas están exclusivamente allí y en el JSON privado; no se incluyeron en Git. Las diez cuentas anteriores se enumeran con contraseña conservada.
- Respaldos: el dump inicial y los dumps por ejecución están en el mismo directorio privado, junto a inventarios y `recorrido-local.json`.
- Consulta adicional de solo lectura: `database/verificar-aprovisionamiento-local.sql`. No hay un script de carga pendiente de ejecutar: la carga principal ya se ejecutó.

Desde la raíz, con los UserSecrets locales existentes y PostgreSQL local disponible:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
# Sin --apply: inventario, sin escrituras a PostgreSQL.
dotnet run --project backend/CampusFix.Api -c Integral --no-launch-profile -- --provision-local --local-approved
# Solo cuando se desea repetir la carga idempotente, con respaldo automático previo.
dotnet run --project backend/CampusFix.Api -c Integral --no-launch-profile -- --provision-local --local-approved --apply
.\scripts\Documentar-AprovisionamientoLocal.ps1
```

Pruebas Angular reproducibles desde `frontend/campusfix-web`: `npm test -- --watch=false --runner-config=vitest-local.config.mts`. El arnés `tests/CampusFix.Checks` requiere `CAMPUSFIX_TEST_POSTGRES` hacia una base local vacía con prefijo `campusfix_qa`; nunca apuntarlo a `campusfix`. Modos: `--postgres`, `--http` y `--postgres --seed-browser`. Para `browser-mejoras.mjs`, seleccionar `CAMPUSFIX_BUILD_CONFIGURATION=Integral` y proporcionar `CAMPUSFIX_PLAYWRIGHT` si Playwright no está instalado en el árbol del proyecto. `browser-integral-local.mjs` usa las cuentas nuevas y el registro privado para reutilizar el ticket adicional; no crea un ticket nuevo en cada ejecución.

## Archivos y commits

- Backend: `Data/LocalProvisioner.cs`, `Migrations/20261010120000_ResourceScope.cs`, `Program.cs`; servicios `CampusService`, `ConfigurationAndKnowledge`, `LocalProvisioning`, `Notifications`, `Organisation`, `Permissions`, `ResourceScope` y `Tickets`.
- Angular: `core/auth.ts` y su prueba; `features/workspace/workspace.ts`, `.html`, `.spec.ts`; `features/insights/insights.ts`, `.html`; `navigation.spec.ts`; `vitest-local.config.mts`.
- Comprobaciones: `tests/CampusFix.Checks/Program.cs`, `MigrationChecks.cs`, `browser-integral-local.mjs`, `browser-mejoras.mjs`.
- Entrega: este informe, `QA_PROGRESO.md`, inventario, consulta SQL y `scripts/Documentar-AprovisionamientoLocal.ps1`.
- Commits de implementación: `10e1f3c` y `898aba3`. El commit de cierre se identifica con `git log -1 --format=%h --grep="test: cerrar implementacion integral local"`.

El respaldo `respaldo-campusfix-qa-antes-mejoras` sigue apuntando a `bc6022e877963e2c664953b670a7e5bcedec5e4f`. No se eliminaron datos existentes, no se modificó Supabase ni producción y no hubo push ni despliegue. No quedan errores funcionales detectados por las pruebas automáticas; la aceptación manual del usuario continúa pendiente.
