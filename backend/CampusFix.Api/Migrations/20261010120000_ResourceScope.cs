using CampusFix.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CampusFix.Api.Migrations;
[DbContext(typeof(CampusFixDbContext))]
[Migration("20261010120000_ResourceScope")]
public class ResourceScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
        -- Only explicit service/team references establish legacy team responsibility.
        WITH links AS (
          SELECT "Data"->>'teamId' team, "Data"->>'responsibleAreaId' area
          FROM "Documentos" WHERE "Kind"='config' AND "Data"->>'kind'='servicios'
          UNION
          SELECT c."Data"->>'teamId',s."Data"->>'responsibleAreaId'
          FROM "Documentos" c JOIN "Documentos" s ON s."Kind"='config' AND s."Id"=c."Data"->>'serviceId'
          WHERE c."Kind"='config' AND c."Data"->>'kind'='categorias'
        ), grouped AS (
          SELECT team,jsonb_agg(DISTINCT area) areas FROM links
          WHERE COALESCE(team,'')<>'' AND COALESCE(area,'')<>'' GROUP BY team
        )
        UPDATE "Documentos" t SET "Data"=t."Data"||jsonb_build_object('sharedAreaIds',g.areas,'_resourceScopeBackfillV1',g.areas),"Version"=t."Version"+1
        FROM grouped g WHERE t."Kind"='config' AND t."Data"->>'kind'='equipos' AND t."Id"=g.team
          AND COALESCE(t."Data"->>'responsibleAreaId','')='' AND NOT(t."Data" ? 'sharedAreaIds');
        CREATE INDEX IF NOT EXISTS "IX_Documentos_ConfigResponsibleArea" ON "Documentos" (("Data"->>'responsibleAreaId')) WHERE "Kind"='config';
        """);
    protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
        UPDATE "Documentos" SET "Data"="Data"-'sharedAreaIds'-'_resourceScopeBackfillV1',"Version"="Version"+1
        WHERE "Kind"='config' AND "Data" ? '_resourceScopeBackfillV1' AND "Data"->'sharedAreaIds'="Data"->'_resourceScopeBackfillV1';
        DROP INDEX IF EXISTS "IX_Documentos_ConfigResponsibleArea";
        """);
}
