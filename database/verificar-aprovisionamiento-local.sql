-- Solo lectura. Ejecutar únicamente en la base local de CampusFix.
SELECT a."Id" AS area_id,a."Data"->>'nombre' AS area,
 count(DISTINCT t."Id") AS solicitudes_iniciales,
 count(DISTINCT t."Data"->>'reportadoPorId') AS solicitantes,
 count(DISTINCT t."Id") FILTER (WHERE t."Data"->>'estado'='1') AS reportadas,
 count(DISTINCT n."Id") AS notificaciones_administrador,
 count(DISTINCT n."Id") FILTER (WHERE n."Data"->>'read'='false') AS no_leidas
FROM "Documentos" a
LEFT JOIN "Documentos" t ON t."Kind"='tickets' AND t."Data"->>'responsibleAreaId'=a."Id"
 AND t."Data"->>'syntheticOrigin'='institutional-2026-10-10-v1' AND t."Data" ? 'provisionKey'
LEFT JOIN "Documentos" n ON n."Kind"='notifications' AND n."Data"->>'ticketId'=t."Id"
 AND n."Data"->>'eventType'='created' AND COALESCE(a."Data"->'managers','[]'::jsonb) ? (n."Data"->>'userId')
WHERE a."Kind"='config' AND a."Data"->>'kind'='areas' AND a."Data"->>'activo'='true'
GROUP BY a."Id",a."Data"->>'nombre' ORDER BY area;

SELECT "Kind",count(*) FROM "Documentos" GROUP BY "Kind" ORDER BY "Kind";
SELECT count(*) AS usuarios FROM "AspNetUsers";
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
-- Debe devolver cero filas: eventos duplicados del lote.
SELECT n."Data"->>'eventKey',n."Data"->>'userId',count(*)
FROM "Documentos" n JOIN "Documentos" t ON t."Kind"='tickets' AND t."Id"=n."Data"->>'ticketId'
WHERE n."Kind"='notifications' AND t."Data"->>'syntheticOrigin'='institutional-2026-10-10-v1'
GROUP BY n."Data"->>'eventKey',n."Data"->>'userId' HAVING count(*)>1;
