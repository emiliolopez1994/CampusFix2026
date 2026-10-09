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

## Siguiente etapa

Etapa 2: verificar permisos del técnico sobre tickets ajenos, transiciones, diagnóstico, solución y notificaciones; aprovechar las pruebas de persistencia ya ejecutadas. Después administración, integración y regresión final.
