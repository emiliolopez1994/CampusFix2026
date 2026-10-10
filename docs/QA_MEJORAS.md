# Mejoras después de QA manual

9 de octubre de 2026. Respaldo intacto `respaldo-campusfix-qa-antes-mejoras` → `bc6022e`; árbol inicial limpio. Documento completo leído. Sin push, despliegues ni escrituras de producción.

## Diagnóstico y decisiones

Reutilizar Documentos JSONB: áreas con managers, servicios/equipos con responsibleAreaId, tickets con snapshot separado del areaId solicitante. Administradores de una o varias áreas; técnicos activos elegibles globalmente salvo especialidad explícita. Sin responsable: clasificación del propietario. No inferir relaciones por nombres/correos. El snapshot local tiene seis servicios sin vínculo explícito; no se modificaron datos reales.

Comentarios normales/notas internas conservan historial sin aviso. Solicitudes de acción tendrán destinatario explícito y nunca notificarán al propio autor. Archivos afectados: servicios de dominio/permisos, migración, registro/perfil, Workspace, indicadores y pruebas.

## Fase 1 verificada

Migración `20261010010000_ServiceRouting`: índices y backfill desde servicios explícitamente vinculados. Prueba de idempotencia y rollback aprobada sin alterar pertenencia, técnico ni historial; 62 comprobaciones de negocio PostgreSQL aprobadas. Configuración admite área responsable y restricción opcional por equipo. Build backend con `-c QA` para conservar la API Debug abierta del usuario. Frontend sin cambios en esta fase.

Scripts revisables: `backend/CampusFix.Api/Migrations/Sql/service-routing-{up,down,report}.sql`. Up aditivo e idempotente; report de solo lectura lista excepciones; down elimina únicamente metadata aún idéntica al backfill e índices agregados. Aplicar con EF o `psql -X -v ON_ERROR_STOP=1 --single-transaction -f ...` a base local; no mezclar SQL manual e historial EF sin revisión. No ejecutar sobre producción sin autorización.

Prueba: `CAMPUSFIX_TEST_POSTGRES` debe apuntar a localhost/base vacía `campusfix_qa*`; `dotnet run --project tests/CampusFix.Checks -c QA --no-restore -- --postgres --migration-only`. Datos ficticios y rollback.

Siguiente: permisos, enrutamiento y UI de configuración/clasificación.

## Fase 2 verificada

Enrutamiento por servicio con snapshot de área responsable; administradores limitados a áreas configuradas incluso con permisos antiguos de alcance global. Solicitante limitado a propios y conocimiento oculto; técnicos asignados/propios. Configuración institucional y responsabilidades reservadas al propietario; administración de personal limitada al ámbito operativo. Directorio técnico global expone únicamente información de asignación y carga, sin permisos/credenciales de otras áreas.

Selector global de técnicos activos con avanzar/solución habilitados; restricción opcional explícita por servicio. Asignación requiere versión y conserva historial con anterior/nuevo/actor/fecha. UI de responsables de área, áreas operativas, servicio responsable y bandeja de clasificación del propietario. Los tickets sin vínculo no se reasignan arbitrariamente; clasificar conserva pertenencia e historial.

Compilaciones correctas; 72 comprobaciones PostgreSQL más migración aprobadas, 24 pruebas Workspace aprobadas. Incluyen acceso/registro cruzado denegado, clasificación y asignación fuera del equipo. Commit anterior `f0e6e30`; siguiente: notificaciones y sincronización.

## Fase 3 verificada

Avisos por creación (solicitante y administradores responsables; propietario solo clasificación), asignación (solicitante/nuevo técnico) y solución (solicitante/administración responsable). Sin aviso por estados intermedios, comentario normal, nota interna o lectura. Acción requerida usa destinatario validado y excluye autor; notas internas nunca se envían a solicitantes. Insert atómico ON CONFLICT por evento/versión/destinatario evita duplicados. Avisos antiguos ruidosos quedan almacenados, fuera de la bandeja activa; historial conservado.

Polling visible cada 15 segundos, una consulta simultánea, actualiza tablero/detalle/indicadores/contador sin borrar borradores y retira accesos revocados. Marcar leído modifica solo lectura/contador, sin recarga secundaria. Recordatorios SLA/asignación de más de 24h: administración responsable, una vez por ticket; propietario solo crítico sin responsable; solicitante/técnico no reciben recordatorios por defecto.

81 comprobaciones PostgreSQL aprobadas; Angular compilado, 30 pruebas Workspace+Insights iniciales y 27 Workspace tras el ajuste final de lectura (31 casos relevantes en total). QA-25 no se asumió: dos PUT de lectura repetidos no producen eventos y la UI no lanza GET adicional. Punto anterior `78d1619`; siguiente: registro/perfil, validaciones, navegación y porcentajes.

## Fase 4 verificada

Registro con catálogo público mínimo de áreas activas y selección obligatoria cuando existe catálogo; perfil de solicitante permite corregir pertenencia con versión concurrente. Los reportes históricos conservan el área original. Formularios muestran errores por campo, foco del primer inválido y botón de envío disponible para explicar errores. Fondo no cierra diálogos; cancelar confirma cambios de formularios y detalle.

Comentarios/adjuntos y soluciones vuelven a la vista de origen solo después de éxito; borradores técnicos ajenos a la acción se conservan en memoria al reabrir. Indicadores muestran porcentajes redondeados; filtros administrativos por área responsable, independientes del área solicitante. Perfil no permite cambiar áreas operativas del personal.

Backend compilado y 85 comprobaciones PostgreSQL más migración aprobadas. Angular compilado y 36 pruebas relevantes aprobadas. Se detectó y corrigió el uso de `_v` en vez de `version` pública en perfil, con prueba específica. Siguiente: regresión integral HTTP, navegador multirrol y entrega.

## Cierre de la fase de mejoras

- Compilaciones Angular y ASP.NET Core QA correctas. Suite Angular completa: **54 pruebas en 8 archivos**, incluida representación 41.2% y total cero.
- **88 comprobaciones de negocio PostgreSQL**, más backfill/idempotencia/rollback de migración; transacción de pruebas revertida.
- **92 comprobaciones HTTP + PostgreSQL** contra middleware, JWT y controladores reales, incluidos recordatorios SLA exclusivos para administración, crítico sin clasificar al propietario y lectura/deduplicación sin eventos secundarios.
- **13 comprobaciones Edge/Playwright multirrol**: registro/área, campos dinámicos, protección del borrador, enrutamiento cruzado, comentario y adjunto, ámbito administrativo, Jorge/Javier elegibles, asignación fuera del equipo original, polling sin notificación intermedia, solución y retorno, valoración, perfil conservando histórico, configuración del propietario y vista móvil sin desbordamiento. Sin errores JavaScript.
- La primera ejecución de navegador terminó en un selector de prueba demasiado estricto para el nombre de un botón con contador. Corregido el selector; la ejecución final completa aprobó. No era un fallo de la aplicación.
- SQL de diagnóstico ejecutado sobre el fixture local: sin excepciones para el servicio configurado. Detecta también áreas inactivas.
- No se probaron entrega de correo externo, lectores de pantalla ni producción. NuGet no pudo consultar su fuente de vulnerabilidades (NU1900); las dependencias locales permitieron compilar y ejecutar las pruebas. No se presenta esa consulta como auditoría de dependencias aprobada.

### Reproducción local

Ejecutar desde la raíz, con PostgreSQL local disponible y bases **vacías y distintas** cuyo nombre empiece por `campusfix_qa`. El arnés rechaza hosts externos. Las bases HTTP/navegador conservan exclusivamente datos ficticios; no reutilizarlas para volver a sembrar.

```powershell
$env:CAMPUSFIX_TEST_POSTGRES='Host=127.0.0.1;Port=55439;Database=campusfix_qa_nueva;Username=campusfix_qa'
dotnet run --project tests/CampusFix.Checks -c QA --no-restore -- --postgres
# Para HTTP: otra base QA vacía, mismo comando con --http en vez de --postgres.
# Para navegador: otra base QA vacía.
dotnet run --project tests/CampusFix.Checks -c QA --no-build -- --postgres --seed-browser
# Tras compilar Angular en frontend/campusfix-web con npm run build:
# CAMPUSFIX_PLAYWRIGHT puede indicar la ruta local del paquete Playwright.
node tests/CampusFix.Checks/browser-mejoras.mjs
```

La nueva prueba sirve directamente `frontend/campusfix-web/dist/campusfix-web/browser` mediante una API QA temporal. No sobrescribe `wwwroot`, `publish` ni la API Debug que estaba abierta. `browser-mejoras.mjs` sustituye el recorrido histórico de `browser-qa.mjs` para esta versión. Las cuentas de fixture usan únicamente dominios example.edu y contraseña ficticia `SoloPruebasQA2026`.

### Activación y datos existentes

La aplicación abierta antes del trabajo sigue ejecutando su compilación anterior. Para revisar las mejoras, iniciar una instancia con el DLL de `backend/CampusFix.Api/bin/QA/net10.0` y el webroot compilado anterior, una conexión **local o aislada** y una clave JWT de pruebas. No copiar una conexión Supabase. Las pruebas ya realizaron ese arranque automáticamente y cerraron sus procesos.

La migración y SQL están entregados, probados y **pendientes de aplicación a cualquier base real**. La API no ejecuta migraciones automáticamente al arrancar. Usar la migración EF `20261010010000_ServiceRouting` con conexión local explícita; alternativamente aplicar `service-routing-up.sql` en una transacción mediante psql. No mezclar ambas vías sin sincronizar el historial EF. El script `down` permite deshacer exclusivamente este backfill conservando cambios posteriores; no es un rollback de las correcciones anteriores del proyecto.

El snapshot existente tenía seis servicios sin área responsable explícita. No se inventaron relaciones ni se alteraron registros reales. Antes de la aceptación con esos datos, el Superusuario debe configurar administradores en **Áreas institucionales**, asociar cada **Servicio** a su área responsable y revisar la bandeja **Clasificación**. Las áreas de pertenencia de solicitantes y las áreas operativas de técnicos son campos distintos. El SQL `service-routing-report.sql` identifica configuraciones pendientes. Los reportes sin relación quedan visibles al propietario para clasificación; no se pierden.

### Comprobación manual complementaria

1. Propietario: asociar dos administradores a una misma área y uno a varias; guardar un servicio responsable. Confirmar que solo esos administradores reciben los nuevos reportes del servicio.
2. Solicitante: pertenencia Bienestar, servicio TI, campo requerido vacío y luego completo; comprobar área de pertenencia y responsable distintas. Corregir perfil y confirmar que solo cambia el valor inicial de futuros reportes.
3. Administración TI: asignar cualquiera de los técnicos activos elegibles, aunque su equipo sugerido sea distinto. Activar la restricción explícita del servicio solo cuando se desee limitarlo al equipo. Otra administración no debe consultar ni mutar el ticket, incluso mediante URL directa.
4. Técnico: avanzar sin generar avisos intermedios; comentario ordinario sin avisos, acción requerida al participante elegido; guardar solución y regresar al tablero. Solicitante y administración reciben el cierre, y el autor no se autonotifica.
5. Dejar abierto un detalle en otra sesión: en hasta 15 segundos se actualizan estado, técnico y contador, conservando borradores. Marcar leído no produce avisos nuevos. Error de red al comentar conserva texto/adjunto; cancelar cambios pide confirmación.

### Commits locales

- `f0e6e30`: dominio y migración de áreas responsables.
- `78d1619`: enrutamiento, ámbito de administración y selección global de técnicos.
- `b0dc902`: notificaciones por evento y sincronización.
- `f3d397f`: perfil, formularios, cierre de diálogos y retorno al guardar.
- El commit de cierre incorpora filtros finales, pruebas multirrol y esta documentación; consultar `git log -1`.

Respaldo final verificado: `respaldo-campusfix-qa-antes-mejoras` = `bc6022e877963e2c664953b670a7e5bcedec5e4f`. No hubo push, despliegues ni escrituras de Supabase.

### Archivos modificados respecto del respaldo

- `backend/CampusFix.Api/CampusFix.Api.csproj`
- `backend/CampusFix.Api/Controllers/CampusController.cs`
- `backend/CampusFix.Api/Data/Store.cs`
- `backend/CampusFix.Api/Migrations/20261010010000_ServiceRouting.cs`
- `backend/CampusFix.Api/Migrations/Sql/service-routing-down.sql`
- `backend/CampusFix.Api/Migrations/Sql/service-routing-report.sql`
- `backend/CampusFix.Api/Migrations/Sql/service-routing-up.sql`
- `backend/CampusFix.Api/Program.cs`
- `backend/CampusFix.Api/Services/CampusService.cs`
- `backend/CampusFix.Api/Services/ConfigurationAndKnowledge.cs`
- `backend/CampusFix.Api/Services/JsonData.cs`
- `backend/CampusFix.Api/Services/Notifications.cs`
- `backend/CampusFix.Api/Services/Organisation.cs`
- `backend/CampusFix.Api/Services/Permissions.cs`
- `backend/CampusFix.Api/Services/Tickets.cs`
- `docs/QA_MEJORAS.md`
- `frontend/campusfix-web/src/app/core/auth.ts`
- `frontend/campusfix-web/src/app/core/login.html`
- `frontend/campusfix-web/src/app/core/login.spec.ts`
- `frontend/campusfix-web/src/app/core/login.ts`
- `frontend/campusfix-web/src/app/features/insights/charts.spec.ts`
- `frontend/campusfix-web/src/app/features/insights/charts.ts`
- `frontend/campusfix-web/src/app/features/insights/insights.html`
- `frontend/campusfix-web/src/app/features/insights/insights.ts`
- `frontend/campusfix-web/src/app/features/workspace/workspace.html`
- `frontend/campusfix-web/src/app/features/workspace/workspace.spec.ts`
- `frontend/campusfix-web/src/app/features/workspace/workspace.ts`
- `QA_PROGRESO.md`
- `tests/CampusFix.Checks/browser-mejoras.mjs`
- `tests/CampusFix.Checks/BrowserFixture.cs`
- `tests/CampusFix.Checks/MigrationChecks.cs`
- `tests/CampusFix.Checks/Program.cs`
- `tests/CampusFix.Checks/QaHttpApi.cs`
- `docs/QA_LOCAL.md`
