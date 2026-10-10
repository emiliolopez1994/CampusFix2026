-- Read-only exceptions for human configuration; no accounts, passwords or data are changed.
SELECT s."Id" AS service_id, s."Data"->>'nombre' AS service_name,
 s."Data"->>'responsibleAreaId' AS responsible_area_id,
 CASE WHEN a."Id" IS NULL THEN 'Área responsable pendiente'
      WHEN COALESCE(a."Data"->>'activo','false') <> 'true' THEN 'Área responsable inactiva'
      ELSE 'Sin administrador activo configurado' END AS exception
FROM "Documentos" s LEFT JOIN "Documentos" a ON a."Kind"='config'
 AND a."Data"->>'kind'='areas' AND a."Id"=s."Data"->>'responsibleAreaId'
WHERE s."Kind"='config' AND s."Data"->>'kind'='servicios'
 AND (a."Id" IS NULL OR COALESCE(a."Data"->>'activo','false') <> 'true' OR NOT EXISTS (
 SELECT 1 FROM "AspNetUsers" u WHERE u."Profile"->>'role'='Administrador'
 AND u."Profile"->>'activo'='true' AND COALESCE(a."Data"->'managers','[]'::jsonb) ? u."Id"));
