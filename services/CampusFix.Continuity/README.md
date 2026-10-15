# Continuidad académica local

Microservicio ASP.NET Core independiente, en `127.0.0.1:5149`, con PostgreSQL propio `campusfix_continuity`. La API principal valida la sesión vigente, el rol Administrador/Superadministrador, el permiso de analítica y las áreas autorizadas. Envía únicamente los IDs, áreas, asuntos y prioridades de los tickets abiertos visibles. El servicio no tiene endpoints para modificar usuarios, solicitudes o áreas de CampusFix.

## Funcionalidad

- Registro y edición de aulas, capacidad, campus, equipamiento y accesibilidad; archivo lógico con protección de relaciones.
- Actividades con horario, participantes y requisitos. No admite solapamientos en la misma aula.
- Vínculo explícito entre una incidencia abierta y el aula afectada. No deduce ubicaciones por coincidencias de texto.
- Análisis de actividades en curso o de los próximos siete días. Agrupa varias incidencias de una misma actividad sin sumar nuevamente sus participantes. Las participaciones entre actividades no equivalen a personas únicas.
- Alternativas del mismo campus y área con capacidad, accesibilidad, equipamiento y disponibilidad compatibles. Las aulas con incidencias abiertas vinculadas se excluyen.
- Aprobación administrativa con nueva comprobación de disponibilidad, reserva dentro del módulo y registro de autor, fecha e incidencias. Versión de concurrencia y bloqueo transaccional impiden aprobar una alternativa que otro administrador acaba de reservar.
- Cancelación antes del inicio, y registro de resultado observado después del inicio. Se conservan minutos de interrupción, observación, autor y fecha. No registra automáticamente resultados exitosos ni modifica calendarios académicos externos.

Las recomendaciones son reglas explicables sobre datos registrados, no un modelo predictivo entrenado. La reserva solo considera este módulo: una ocupación externa no registrada no puede verificarse. El administrador debe comunicar los cambios de aula a sus participantes.

## Preparación y arranque

Desde la raíz del repositorio, con PowerShell 7 y PostgreSQL 18 local:

```powershell
dotnet restore services/CampusFix.Continuity/CampusFix.Continuity.csproj
pwsh -File scripts/Preparar-ContinuidadLocal.ps1 -SeedExamples
pwsh -File scripts/Iniciar-ContinuidadLocal.ps1
```

El preparador verifica loopback y la base origen `campusfix`, respalda antes de actuar, crea exclusivamente `campusfix_continuity` y su tabla `continuity_state`. Conserva intacta la base principal. Crea `Continuity:ApiKey` y `ConnectionStrings:Continuity` en los secretos locales existentes fuera de Git. `-SeedExamples` es opcional. Puede repetirse: no duplica ni restablece registros editados, decisiones o datos archivados.

La API principal debe estar compilada con esta integración y ejecutarse en Development para leer sus secretos. Para iniciarla, en otra terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project backend/CampusFix.Api/CampusFix.Api.csproj --no-launch-profile --urls http://localhost:5137
```

Entra como Administrador o Superusuario y abre **Continuidad académica**. Angular puede servirse desde `wwwroot` o su servidor de desarrollo habitual. El microservicio de informes sigue siendo independiente y se inicia con su script habitual.

## Ejemplos cargados el 14 de octubre de 2026

Se aprovisionaron tres aulas y dos actividades por cada una de las ocho áreas existentes: **24 aulas y 16 actividades**. Ocho vínculos ficticios relacionan la sala 01 de cada área con un ticket abierto que ya tenía procedencia sintética; no se cambió ningún ticket. Las aulas tienen 45, 45 y 20 plazas, proyector, wifi, pizarra y accesibilidad. Las actividades tienen 30 y 25 participantes. Se aprobaron desde el navegador una actividad y su alternativa en Dirección Académica como comprobación funcional.

Los IDs son estables y `origin=synthetic-continuity-v1` identifica cada ejemplo; la interfaz muestra una advertencia de procedencia. Los horarios se fijan una vez, relativos al primer aprovisionamiento. No se desplazan al reiniciar: para otra fecha de presentación, edita los horarios desde Actividades (cancela antes cualquier decisión pendiente vinculada). Los ejemplos no deben presentarse como interrupciones reales ni como resultados medidos de una universidad.

## Contrato y persistencia

- Angular: `GET /api/continuity` y `POST /api/continuity`, autenticados por CampusFix.
- Servicio: `GET /health`; `POST /v1/view` y `/v1/command`, protegidos con `X-Continuity-Key` privado.
- El contexto `scope` lo construye exclusivamente la API: `{ actor, areas, tickets }`. Angular nunca recibe la clave interna.
- Operaciones: `room.save/archive`, `activity.save/archive`, `link.save/archive`, `decision.approve/cancel/outcome`; cada cambio exige la versión de estado que recibió el cliente.
- Los registros se guardan como JSONB en la base independiente; cada mutación bloquea la fila y confirma todo en una transacción. No se eliminan físicamente registros. La versión es global al microservicio: un cambio concurrente de otra área puede pedir actualizar.
- Una caída devuelve 503 únicamente en Continuidad. No bloquea login, tickets, informes ni demás módulos.

## Verificación

`dotnet run --project tests/CampusFix.Continuity.Checks` ejecuta las comprobaciones focalizadas. Para incluir PostgreSQL, `CONTINUITY_TEST_CONNECTION` debe apuntar a una base **vacía**, local y con nombre `campusfix_continuity_qa...`. Nunca utiliza la base funcional para crear fixtures.

Resultados de esta entrega: **19 comprobaciones** de reglas y PostgreSQL correctas; autenticación HTTP por rol (Administrador acotado a una área, Técnico/Solicitante 403, llamada directa sin clave 401); aprobación mediante Edge, recarga y persistencia tras reinicio real; segunda carga con hash idéntico; caída aislada comprobada. Interfaz comprobada en escritorio y móvil, sin errores JavaScript. El resultado observado se probó en la base QA con reloj controlado, sin inventar un resultado exitoso de las actividades locales futuras. Acceso global del Superusuario implementado por el filtro del servidor; la cuenta original conserva su contraseña y su validación interactiva sigue a cargo del usuario.

ASP.NET Core y Angular compilaron. NuGet no pudo consultar su fuente de vulnerabilidades por la restricción de red (`NU1900`); se utilizó Npgsql 10.0.3 ya presente en el proyecto. No hubo push ni despliegue.

Respaldos, exportación de áreas y capturas locales privadas: `%LOCALAPPDATA%\CampusFix\continuity`. Los archivos de acceso de los usuarios no cambian. No hay scripts SQL pendientes: la base y la tabla ya fueron creadas por el preparador.
