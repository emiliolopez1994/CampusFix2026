-- Only undo metadata still identical to this backfill. Preserve subsequent manual routing.
UPDATE "Documentos" SET "Data"="Data" - 'responsibleAreaId' - 'responsibleAreaName' - '_routingBackfillV1',
 "Version"="Version"+1 WHERE "Kind"='tickets' AND "Data" ? '_routingBackfillV1'
 AND "Data"->>'responsibleAreaId'="Data"->'_routingBackfillV1'->>'areaId'
 AND "Data"->>'responsibleAreaName'="Data"->'_routingBackfillV1'->>'areaName';
DROP INDEX IF EXISTS "IX_Documentos_TicketResponsibleArea";
DROP INDEX IF EXISTS "IX_Documentos_Service";
