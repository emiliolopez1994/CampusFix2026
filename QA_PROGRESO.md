# Progreso QA CampusFix

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

## Siguiente etapa

Etapa 5: regresión final. Instrucciones reproducibles y límites en `docs/QA_LOCAL.md`.

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
