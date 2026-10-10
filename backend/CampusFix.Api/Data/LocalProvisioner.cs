using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using CampusFix.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static CampusFix.Api.Services.J;

namespace CampusFix.Api.Data;
public static class LocalProvisioner
{
    public static async Task Run(string[] args,IServiceProvider services,IConfiguration configuration,IHostEnvironment environment)
    {
        Require(environment.IsDevelopment()&&args.Contains("--local-approved"),400,"Requiere Development y --local-approved. Nunca se ejecuta al iniciar la API.");
        var cs=new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("CampusFix"));
        Require(cs.Host is "localhost" or "127.0.0.1" or "::1",400,"Aprovisionamiento permitido únicamente en loopback local.");
        var addresses=await Dns.GetHostAddressesAsync(cs.Host);Require(addresses.Length>0&&addresses.All(IPAddress.IsLoopback),400,"El host no resuelve exclusivamente a loopback.");
        var db=services.GetRequiredService<CampusFixDbContext>();await db.Database.OpenConnectionAsync();
        var endpoint=await db.Database.SqlQueryRaw<string>("SELECT host(inet_server_addr()) AS \"Value\"").SingleAsync();Require(IPAddress.TryParse(endpoint,out var address)&&IPAddress.IsLoopback(address),400,"El servidor no es local.");
        var privateDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CampusFix","integral-2026-10-10");Directory.CreateDirectory(privateDir);
        var store=services.GetRequiredService<IStore>();var before=await Inventory(store);var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
        await File.WriteAllTextAsync(Path.Combine(privateDir,"inventario-"+stamp+".json"),before.ToJsonString(new(){WriteIndented=true}));
        var pending=(await db.Database.GetPendingMigrationsAsync()).ToArray();
        Console.WriteLine($"Destino local verificado: {cs.Host}:{cs.Port}/{cs.Database}. Áreas activas: {before.I("activeAreas")}. Migraciones pendientes: {pending.Length}.");
        if(!args.Contains("--apply")){Console.WriteLine("Previsualización: completar únicamente recursos faltantes y cinco solicitudes por área; no se modificó la base.");return;}
        var backup=Path.Combine(privateDir,"respaldo-"+stamp+".dump");var pgDump=configuration["LocalProvisioning:PgDump"]??@"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe";
        var start=new ProcessStartInfo(pgDump){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
        foreach(var arg in new[]{"-h",cs.Host,"-p",cs.Port.ToString(),"-U",cs.Username!,"-d",cs.Database!,"-Fc","-f",backup})start.ArgumentList.Add(arg);
        start.Environment["PGPASSWORD"]=cs.Password;using(var process=Process.Start(start)!){await process.WaitForExitAsync();Require(process.ExitCode==0&&new FileInfo(backup).Length>0,500,"El respaldo local falló; no se inició la carga.");}
        await db.Database.MigrateAsync();await using var transaction=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261010,37)");
        var accessPath=Path.Combine(privateDir,"accesos-locales.json");var credentials=File.Exists(accessPath)?JsonNode.Parse(await File.ReadAllTextAsync(accessPath))!.AsArray():new JsonArray();
        // Suppress external mail for the command only, without altering the user's mail configuration.
        configuration["LocalProvisioning:SuppressMail"]="true";
        var report=await services.GetRequiredService<CampusService>().ProvisionInstitution(credentials);
        var all=await store.All("tickets");var notifications=await store.All("notifications");
        foreach(var area in report.A("areas").Items()){
            var ids=area.A("ticketIds").Select(x=>x!.ToString()).ToHashSet();var tickets=all.Where(t=>ids.Contains(t.S("id"))).ToList();
            Require(tickets.Count==5&&tickets.Select(t=>t.S("reportadoPorId")).Distinct().Count()==5&&tickets.All(t=>t.S("responsibleAreaId")==area.S("areaId")),500,"No se cumplió la cobertura de cinco solicitudes distintas por área.");
            foreach(var manager in area.A("administratorIds"))Require(notifications.Count(n=>ids.Contains(n.S("ticketId"))&&n.S("eventType")=="created"&&n.S("userId")==manager!.ToString())==5,500,"No se verificaron cinco eventos iniciales del administrador.");
        }
        var after=await Inventory(store);report["before"]=before;report["after"]=after;report["backup"]=backup;report["database"]=cs.Database;report["host"]=cs.Host;
        // Persist generated credentials before committing so an interrupted process cannot lose them.
        await File.WriteAllTextAsync(accessPath,credentials.ToJsonString(new(){WriteIndented=true}));await transaction.CommitAsync();
        await File.WriteAllTextAsync(Path.Combine(privateDir,"resultado-"+stamp+".json"),report.ToJsonString(new(){WriteIndented=true}));
        await File.WriteAllTextAsync(Path.Combine(privateDir,"resultado-actual.json"),report.ToJsonString(new(){WriteIndented=true}));
        Console.WriteLine($"Carga confirmada: {report.A("areas").Count} áreas, {report.A("areas").Count*5} solicitudes del lote. Respaldo e inventarios: {privateDir}. Credenciales nuevas guardadas privadamente; no se imprimen.");
    }
    static async Task<JsonObject> Inventory(IStore store){var config=await store.All("config");return Obj(new{activeAreas=config.Count(c=>c.S("kind")=="areas"&&c.B("activo")),config=config.Select(c=>Obj(new{id=c.S("id"),kind=c.S("kind"),nombre=c.S("nombre"),activo=c.B("activo"),parentId=c.S("parentId"),responsibleAreaId=c.S("responsibleAreaId"),sharedAreaIds=c.A("sharedAreaIds").DeepClone(),managers=c.A("managers").DeepClone(),members=c.A("members").DeepClone(),serviceId=c.S("serviceId"),teamId=c.S("teamId")})).ToArray(),users=(await store.All("users")).Select(Permissions.Public).ToArray(),tickets=(await store.All("tickets")).Count,notifications=(await store.All("notifications")).Count});}
}
