# Progreso QA CampusFix

## QA-41 — CRUD administrativo y eliminación lógica

Se conservó QA-40. Se añadieron DELETE lógicos para áreas, sedes, ubicaciones, equipos, servicios, categorías, calendarios, metas, usuarios, artículos y conexión de correo. Eliminado/fechaEliminacion/eliminadoPorId se guardan en JSONB; no hace falta migración estructural. Catálogos operativos excluyen eliminados y las operaciones directas los rechazan. Dependencias activas bloquean el retiro con 409, sin cascadas ni reasignaciones. Usuarios: protección de cuenta propia/último Superadministrador; administradores no pueden retirar cuentas compartidas con otras áreas. Organización y cuatro roles esenciales permanecen protegidos; se agregaron perfiles reutilizables de permisos con CRUD, desactivación y aplicación explícita sobre la configuración seleccionada.

Angular incorpora la confirmación común y refresco de listados. Técnico: edición exclusiva de mensajes propios, fecha original preservada y marca Editado, sin borrado. Solicitante: edición de título/ubicación/descripción en solicitudes propias Reportado y sin asignar; relaciones y prioridad permanecen protegidas. Revisión local: 62 usuarios, 8 áreas, 68 tickets, 6 artículos; no se eliminó ningún registro ni se cambiaron credenciales. Compilación API y compilador Angular; sin batería extensa. Pendiente manual: confirmaciones, bloqueos por dependencias/ámbito, perfiles de permisos, correo, edición propia y eliminación de registros creados por el usuario para verificar.

## Ajuste QA-39 y QA-40 — 10-10-2026

Jorge Romero tenía pertenencia a TI pero ninguna autorización operativa ni equipo. Con respaldo local previo se añadió únicamente TI a operationalAreas de Jorge; Javier y las demás cuentas permanecen intactos. Los seis artículos conservan sus relaciones: Gestión de accesos es un servicio de TI, no la cuenta Gestión de incidencias. Se conserva QA-39 sin cambios de filtrado.

QA-40 reutiliza eliminado/fechaEliminacion en JSONB de PostgreSQL y añade eliminadoPorId al ejecutar DELETE; conserva registro e historial. Autorización explícita: Solicitante propio, Administrador de su área, Superadministrador global; Técnico siempre denegado. Exige versión, bloquea acceso y operaciones posteriores, conserva filtros de informes/listados y excluye eliminados del directorio operativo. Angular muestra Eliminar solicitud con confirmación, retira inmediatamente el ticket y refresca el listado. No requiere migración estructural. No se eliminó ninguna solicitud durante la implementación. Compilación API y revisión de tipos/plantillas Angular correctas; sin batería extensa. La eliminación real queda para la comprobación manual del usuario.

## Credenciales académicas locales — 10-10-2026

Actualizadas exclusivamente 52 cuentas sintéticas; IDs, perfiles, roles y relaciones conservados. Correos calculados desde la primera letra y segunda palabra, sin tildes, con sufijos mínimos para colisiones. Contraseña fija solicitada; excepción autorizada `Ade00123456#` en siete cuentas «Administración de …». Se mantienen 62 usuarios, cero correos duplicados y las diez filas originales intactas. Respaldo previo: `%LOCALAPPDATA%\CampusFix\integral-2026-10-10\antes-credenciales-20261010-172928.dump`. Listado privado: `ACCESOS_LOCALES_ACTUALIZADOS.md` en ese mismo directorio.

Login ajustado para buscar correo sin cambiar ID; registro impide duplicados por correo e ID. Política de nuevas contraseñas: 12–128 caracteres, mayúscula, minúscula, número y especial; no invalida contraseñas originales al iniciar sesión. Aprovisionador reutiliza los IDs estables y sincroniza el correo actual en su listado, sin regenerar claves existentes. Script local ejecutado: `scripts/Actualizar-CredencialesLocales.py`; usa conexión PG* exclusivamente localhost/campusfix, respaldo y transacción. No se repitió el aprovisionamiento institucional.

Verificación acotada: API compilada sin errores; login y auth/me reales de Administrador, Técnico y Solicitante correctos; contraseñas cortas/sin especial rechazadas y correo existente devuelve 409 sin crear usuario. Huellas completas de documentos (incluidos tickets, recursos, historial y notificaciones) y roles idénticas; únicamente se agregan sesiones de los accesos probados. No se repitió la regresión integral. API local ejecutándose con configuración Credenciales; Angular de desarrollo conserva su proceso. Sin producción, push ni despliegue.

## Especificación integral 10-10-2026 — implementación cerrada; aceptación manual del usuario

QA-30/31/34/35/36/37 implementados; carga EJECUTADA en localhost:5432/campusfix con respaldo. 8 áreas × 5 solicitudes iniciales, 62 usuarios, 116 recursos, 68 tickets incluyendo uno adicional solucionado. Repetición sin duplicados y persistencia tras reiniciar. Regresión: 58 Angular, 98 negocio PostgreSQL, dos migraciones, 102 HTTP, 13 navegador multirrol y 8 instalación local. Accesos privados organizados y evidencia en docs/QA_INTEGRAL_2026-10-10.md e inventario adjunto. Commits 10e1f3c, 898aba3 y cierre local. El usuario asumió expresamente la verificación manual; las sesiones de cuentas existentes no se declaran probadas en la instalación principal. No repetir auditorías ya verificadas. Producción y etiqueta bc6022e intactas.


## Nueva fase de mejoras posteriores a QA manual

Respaldo `respaldo-campusfix-qa-antes-mejoras` verificado en `bc6022e`, sin alterarlo. Evidencias nuevas: `docs/QA_MEJORAS.md`. Fases 1–5 completadas y verificadas: 54 pruebas Angular, 88 de negocio PostgreSQL más migración, 92 HTTP y 13 navegador multirrol. Commits: `f0e6e30`, `78d1619`, `b0dc902`, `f3d397f` y commit local de cierre. No repetir auditoría: cobertura y reproducción en `docs/QA_MEJORAS.md`. Pendiente externo: configurar las relaciones institucionales reales y aplicar la migración únicamente cuando corresponda; no se tocaron datos reales ni la instancia Debug abierta. Los resultados siguientes corresponden a la fase anterior validada por el usuario.

Fecha: 2026-10-09 (America/Guayaquil). Respaldo `respaldo-campusfix-funcional` confirmado; árbol inicial limpio, base `3862abd`.
No se ejecutaron push, despliegues ni operaciones sobre Supabase. El informe DOCX se consultó una vez; sus hipótesis no se consideran fallos demostrados.

## Etapa 1 — Solicitante: verificada localmente

- Causa reproducida del fallo de creación: SQL de `Store.Next` sin comilla de cierre en `RETURNING "Value"` (PostgreSQL 42601). Corregido con literal multilínea.
- Modal: fondo no cierra; Escape/X/Cancelar confirman descarte si hay cambios. Borrador de nuevo ticket recuperable por usuario en sessionStorage de la pestaña; se elimina al guardar o descartar. No se promete recuperación tras cerrar la pestaña.
- Guías relacionadas ocultas al Solicitante en creación y detalle. Se conserva el módulo de conocimiento y su autorización existente (el requisito no solicita eliminarlo).
- Validación explícita de campos y formatos, soporte de cero/decimales; solo áreas autorizadas seleccionables. La selección explícita «Pendiente de clasificación» ya no se sustituye por el área principal.
- Errores HTTP accionables y referencia correlacionada en errores 500. Un fallo de recarga tras guardar no devuelve al formulario para reenviar el ticket.
- Archivos: `Data/Store.cs`, `Program.cs`, `Services/Tickets.cs` del backend; `workspace.ts/html/spec.ts` del frontend; `tests/CampusFix.Checks/Program.cs`.
- Verificación: build Angular producción correcto; build ASP.NET correcto; 20 pruebas Workspace correctas; 45 comprobaciones de negocio correctas en memoria y PostgreSQL 18 aislado, con rollback de datos sintéticos. Estas comprobaciones incluyen roles existentes, solución, notificaciones y persistencia.
- Infraestructura: Vitest e initdb no arrancaron correctamente dentro del sandbox; ejecución local ampliada resolvió la limitación. Aviso NU1900: no se pudo consultar el feed de vulnerabilidades NuGet; no impidió compilar.
- Último punto estable: commit de esta etapa, localizable con `git log -1 --format=%h --grep="fix: corregir creacion y formulario de tickets solicitante"`.
- Pendiente externo: reproducción autenticada de producción y correlación con logs Render; no realizada porque no se deben crear datos reales. QA-01 está reproducido y corregido localmente, no desplegado.

## Etapa 2 — Técnico: verificada localmente

- Corregida pérdida de solución/respuesta/adjunto pendientes al enviar mensajes, cambiar estado o refrescar el detalle tras conflicto. Cada operación limpia únicamente su contenido enviado; refrescar conserva borradores y adopta la versión actual.
- Archivos: `workspace.ts`, `workspace.spec.ts`, `tests/CampusFix.Checks/Program.cs`.
- Verificación: compilación Angular y backend correcta; 22 pruebas Workspace; 51 comprobaciones PostgreSQL, incluyendo acceso directo ajeno denegado, estado/solución/mensajes ajenos denegados, prohibición de asignación y notificación al técnico asignado. Diagnóstico interno, flujo completo, solución y persistencia ya cubiertos.
- Último commit estable anterior: `782fab4` (etapa 1). Punto estable de etapa 2: `git log -1 --format=%h --grep="fix: conservar borradores durante atencion tecnica"`.
- Pendiente: recorrido visual autenticado de producción y entrega real de correo; no ejecutados.

## Estado final

Etapas 1–5 completadas dentro del alcance local probado. Instrucciones reproducibles y límites en `docs/QA_LOCAL.md`. No quedan correcciones locales a medio implementar. Siguiente paso externo: despliegue y validación con cuentas de prueba solo cuando se autoricen; no se ha realizado.

## Etapa 3 — Administración: verificada localmente

- Editor de conocimiento: ocultar edición de publicaciones/archivados a técnicos, como exige el servidor; conservar edición de borradores e historial. Valoración visible solo con permiso conocimiento.
- Archivos: `workspace.ts/html/spec.ts`, `tests/CampusFix.Checks/Program.cs`.
- Verificación: compilación Angular y backend correcta; 27 pruebas de Workspace e indicadores; 62 comprobaciones PostgreSQL. Incluyen gestión de solicitantes, alta de técnico por propietario, cuentas protegidas, configuración, analítica restringida, publicaciones y conflictos de versión. No se cambiaron permisos existentes que ya funcionaban.
- Punto estable anterior `3b12b8c`; commit de etapa: `git log -1 --format=%h --grep="fix: alinear acciones de conocimiento con permisos"`.
- QA-15: áreas/equipo/informes comparten deliberadamente el permiso analítica en menú y rutas. No se inventaron permisos nuevos sin requisito de producto.
- Pendiente: correo real y recorridos de producción; no ejecutados.

## Etapa 4 — Integración: verificada localmente

- Añadido modo `--http` que inicia API real con puerto/clave efímeros y base local vacía; comprueba middleware, controladores, transacciones y PostgreSQL. Restricción preventiva de pruebas PostgreSQL a localhost y nombres `campusfix_qa*`.
- Archivos: `tests/CampusFix.Checks/Program.cs`, `QaHttpApi.cs`, `docs/QA_LOCAL.md`.
- Verificación: 66 comprobaciones HTTP + PostgreSQL correctas; backend recompilado. Compilación Angular vigente de etapa 3 (sin cambios frontend en esta etapa).
- Lectura pública: login Vercel 200; health por Vercel y Render 200/Conectado. No se efectuaron POST autenticados en producción.
- Vercel rewrites, Docker/Render, proxy de desarrollo y JWT revisados. No se requieren cambios de configuración detectados; el arreglo SQL sigue pendiente de despliegue autorizado.
- Punto estable anterior `db8c27b`; commit de etapa: `git log -1 --format=%h --grep="test: verificar API real con PostgreSQL aislado"`.

## Etapa 5 — Regresión final: verificada localmente

- Ajuste final: al cambiar servicio, guardar inmediatamente el borrador sin los campos del servicio anterior; regresión añadida.
- Archivos: `workspace.ts`, `workspace.spec.ts`, `tests/CampusFix.Checks/browser-qa.mjs`, documentación QA.
- Angular: compilación producción correcta; suite completa **47/47**, siete archivos (formularios, navegación, acceso, temas, indicadores y CSV).
- ASP.NET: compilación Debug sin errores/advertencias; **62/62** comprobaciones en memoria. Evidencia PostgreSQL de etapas 3–4 vigente: **62/62** de servicio y **66/66** por HTTP real (sin cambios posteriores en backend).
- Edge headless con Angular compilado, ASP.NET y PostgreSQL local: **7/7** comprobaciones. Registro/login, fondo/Escape y guías, borrador tras recarga, POST 201 y creación persistente, respuesta tras navegación, diálogo móvil y ausencia de errores JavaScript.
- La primera medición móvil se hizo durante la transición CSS de la barra lateral. Se ajustó la espera del test al diseño estable y pasó; no se modificó CSS por ese resultado transitorio.
- Compilación Release local actualizada en `publish/`; Angular actualizado en `backend/CampusFix.Api/wwwroot/`. Hash de `publish/wwwroot/index.html` coincide con build Angular. Son salidas locales ignoradas por Git, no despliegues.
- Puntos recuperables: etapa 1 `782fab4`, etapa 2 `3b12b8c`, etapa 3 `db8c27b`, etapa 4 `e3dde54`. Último punto estable: **HEAD**, commit `test: cerrar regresion QA local verificada` (resolver hash con `git log -1 --format=%h`).
- PostgreSQL de QA: clúster propio en `tests/CampusFix.Checks/obj/qa-postgres`, loopback:55439; se detiene al terminar. Conserva únicamente datos sintéticos de HTTP/navegador en bases `campusfix_qa*`.
- Límites pendientes: no despliegue, no prueba autenticada de producción, no entrega real de correo ni auditoría completa de accesibilidad/lector de pantalla. La validación visual automatizada de navegador se centró en Solicitante; los cuatro roles se comprobaron por lógica y HTTP.


## Microservicio adicional de informes imprimibles — 2026-10-13
- Añadido CampusFix.Reports (.NET 10), proceso independiente en loopback:5148, sin acceso a PostgreSQL ni persistencia de documentos. Nueva descarga HTML imprimible desde Centro de informes; CSV y módulos existentes conservados.
- API: sesión y permiso analitica vigentes, selección limitada a tickets visibles/no eliminados, comunicación interna con clave privada, timeout 8 s y error 503 aislado.
- Verificado: compilación API/microservicio sin advertencias; Angular ngc y build development correctos; descarga autenticada con tres tickets; selección inaccesible 409; acceso directo sin clave 401; Solicitante sin analitica 403; microservicio detenido devuelve 503 mientras listado de tickets sigue idéntico. Servicio reiniciado y health correcto.
- Interfaz compilada local servida en http://localhost:5137. Build optimizado no completado por DNS de fonts.googleapis.com; no se cambió la configuración de fuentes. Impresión física/PDF y revisión visual final pendientes de usuario; HTML generado en carpeta privada integral-2026-10-10.
- Sin migraciones, cambios de credenciales de usuarios, modificaciones de tickets/áreas/notificaciones, push ni despliegues. Se generaron/cerraron sesiones normales para comprobar permisos. Clave independiente Reports:ApiKey fuera de Git; arranque documentado en services/CampusFix.Reports/README.md.

## Ajuste del carrusel de acceso — 2026-10-14
- Imágenes suministradas ajustadas al ancho y alto completos del panel, sin recorte por object-fit ni franja adicional de títulos. Se conserva la composición original de la primera diapositiva.
- Intervalo reducido de 7 a 3,5 segundos; se conserva pausa por interacción y preferencia de movimiento reducido.
- Compilación Angular development correcta y salida local wwwroot actualizada. Sin cambios de API o datos.

## Rapidez de navegación — 2026-10-14
- Causa medida: /api/data recalculaba permisos de los usuarios para cada ticket vencido e intentaba crear de nuevo recordatorios SLA ya existentes al navegar.
- Optimización puntual en Notifications.cs: carga compartida de datos durante una sola consulta, permisos reutilizados dentro de esa evaluación y omisión de recordatorios existentes. CreateOnce conserva protección ante concurrencia; no hay caché entre peticiones ni cambios de autorización.
- Misma cuenta Administrador: antes 1344/1291 ms; después 205 ms inicial y 121/119 ms repetidos. Se mantienen 6 tickets y 12 notificaciones. Mejora aproximada 91% en las consultas repetidas; no constituye una medición de todos los roles o de renderizado del navegador.
- Compilación Navigation correcta, cero advertencias/errores; API local reiniciada. Sin cambios de frontend, credenciales, producción ni despliegues.

## Microservicio de continuidad académica — 2026-10-14
- Añadido CampusFix.Continuity (loopback:5149), PostgreSQL independiente campusfix_continuity y sección Angular /continuidad para administradores de área y Superusuario.
- Registro/edición/archivo de aulas y actividades, vínculos explícitos de incidencias, impacto de los próximos 7 días, alternativas compatibles, aprobación con bloqueo de dobles reservas, cancelación y resultado observado. Permisos y tickets abiertos se validan desde la API principal; no se modifican los tickets.
- Carga autorizada ejecutada: 24 aulas, 16 actividades, 8 vínculos sintéticos con tickets previamente sintéticos de las ocho áreas. Una alternativa de Dirección Académica aprobada mediante navegador. Procedencia interna synthetic-continuity-v1 y aviso visible de datos ficticios.
- Respaldos antes de crear/cargar: C:\Users\lio13\AppData\Local\CampusFix\continuity. Segunda carga verificada con hash idéntico; decisión y datos conservados después de reiniciar el proceso.
- Verificación focalizada: 19 comprobaciones de reglas y PostgreSQL QA; administrador ve solo su área; Técnico/Solicitante 403, servicio sin clave 401. Navegador: aprobación y actualización de impacto, recarga, vista escritorio/móvil; sin errores JS. La medición móvil inicial ocurrió durante la transición del menú; tras estabilizar no hay desbordamiento, sin cambios ajenos de CSS.
- Caída de continuidad devuelve 503 aislado; API principal sigue sirviendo tickets. Servicio reiniciado y disponible. Angular compilado y wwwroot actualizado; API en configuración Continuity.
- Límites: horarios de ejemplo fijados al primer aprovisionamiento (editables); no son horarios reales ni se regeneran automáticamente. Confirmación de resultado comprobada en QA con reloj controlado. Superusuario original: validación interactiva manual del usuario. NuGet advierte NU1900 por fuente de vulnerabilidades inaccesible; compilaciones correctas. No push ni despliegues; archivos package-lock ajenos conservados.
