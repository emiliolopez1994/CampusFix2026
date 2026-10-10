using CampusFix.Api.Data;
using CampusFix.Api.Migrations;
using CampusFix.Api.Services;
using Microsoft.EntityFrameworkCore;
using static CampusFix.Api.Services.J;

static class MigrationChecks
{
    public static async Task Run(CampusFixDbContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var store = new Store(db);
        await store.Save("config", Obj(new {id="migration-area",kind="areas",nombre="TI QA",activo=true}));
        await store.Save("config", Obj(new {id="migration-service",kind="servicios",responsibleAreaId="migration-area"}));
        await store.Save("tickets", Obj(new {id="migration-ticket",serviceId="migration-service",areaId="requester-area",tecnicoId="existing-tech",historial=new[]{new {action="Preservar"}}}));
        await db.Database.ExecuteSqlRawAsync(ServiceRouting.Script("up"));
        var first = (await store.Get("tickets", "migration-ticket"))!;
        if(first.S("responsibleAreaId")!="migration-area"||first.S("areaId")!="requester-area"||first.S("tecnicoId")!="existing-tech")throw new Exception("Routing backfill changed existing ownership.");
        await db.Database.ExecuteSqlRawAsync(ServiceRouting.Script("up"));
        var second = (await store.Get("tickets", "migration-ticket"))!;
        if(first.ToJsonString()!=second.ToJsonString())throw new Exception("Routing backfill is not idempotent.");
        await db.Database.ExecuteSqlRawAsync(ServiceRouting.Script("down"));
        var reverted = (await store.Get("tickets", "migration-ticket"))!;
        if(reverted.ContainsKey("responsibleAreaId")||reverted.A("historial").Count!=1)throw new Exception("Routing rollback changed history.");
        await tx.RollbackAsync();
        Console.WriteLine("PASS routing migration backfill, idempotence and rollback preserve membership, technician and history");
    }
}
