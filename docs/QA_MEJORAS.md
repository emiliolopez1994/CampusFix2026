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
