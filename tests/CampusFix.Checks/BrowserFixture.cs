using CampusFix.Api.Data;
using CampusFix.Api.Models;
using Microsoft.AspNetCore.Identity;
using static CampusFix.Api.Services.J;

static class BrowserFixture
{
    public static async Task Seed(IStore store)
    {
        // Synthetic accounts, only callable after the harness localhost/empty DB guards.
        var hasher=new PasswordHasher<Usuario>();
        foreach(var (id,role,name) in new[]{("owner","Superadministrador","Propietario QA"),("admin","Administrador","Administrador TI"),("other","Administrador","Administrador ajeno"),("jorge","Técnico","Jorge QA"),("javier","Técnico","Javier QA")})
            await store.Save("users",Obj(new{id=id+"@example.edu",correo=id+"@example.edu",nombre=name,role,activo=true,passwordHash=hasher.HashPassword(new Usuario(),"SoloPruebasQA2026#"),stamp=Id(),areas=new[]{"bienestar"},areaId="bienestar",operationalAreas=new[]{"ti"}}));
        await store.Save("config",Obj(new{id="bienestar",kind="areas",nombre="Bienestar",activo=true,managers=Array.Empty<string>()}));
        await store.Save("config",Obj(new{id="ti",kind="areas",nombre="Tecnología",activo=true,managers=new[]{"admin@example.edu"}}));
        await store.Save("config",Obj(new{id="mantenimiento",kind="areas",nombre="Mantenimiento",activo=true,managers=new[]{"other@example.edu"}}));
        await store.Save("config",Obj(new{id="soporte",kind="equipos",nombre="Soporte original",activo=true,members=new[]{"javier@example.edu"}}));
        await store.Save("config",Obj(new{id="service",kind="servicios",nombre="Soporte informático",activo=true,teamId="soporte",responsibleAreaId="ti",fields=new[]{new{id="asset",label="Equipo afectado",type="text",required=true,@internal=false,options=Array.Empty<string>()}}}));
        Console.WriteLine("Browser fixture ready in isolated local QA database.");
    }
}
