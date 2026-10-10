# Progreso QA CampusFix

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

