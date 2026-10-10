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
