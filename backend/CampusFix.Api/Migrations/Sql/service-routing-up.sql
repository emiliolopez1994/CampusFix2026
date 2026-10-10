-- Additive routing metadata. Never infer service responsibility from requester membership.
CREATE INDEX IF NOT EXISTS "IX_Documentos_TicketResponsibleArea"
ON "Documentos" (("Data"->>'responsibleAreaId')) WHERE "Kind"='tickets';
CREATE INDEX IF NOT EXISTS "IX_Documentos_Service"
ON "Documentos" (("Data"->>'serviceId')) WHERE "Kind"='tickets';
WITH routes AS (
 SELECT s."Id" AS service_id, a."Id" AS area_id, a."Data"->>'nombre' AS area_name
 FROM "Documentos" s JOIN "Documentos" a
 ON a."Kind"='config' AND a."Data"->>'kind'='areas'
 AND a."Id"=s."Data"->>'responsibleAreaId'
 WHERE s."Kind"='config' AND s."Data"->>'kind'='servicios'
)
UPDATE "Documentos" t SET "Data"=t."Data" || jsonb_build_object(
 'responsibleAreaId',r.area_id,'responsibleAreaName',r.area_name,
 '_routingBackfillV1',jsonb_build_object('areaId',r.area_id,'areaName',r.area_name)),
 "Version"=t."Version"+1
FROM routes r WHERE t."Kind"='tickets' AND t."Data"->>'serviceId'=r.service_id
 AND NOT (t."Data" ? 'responsibleAreaId');
