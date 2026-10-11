using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;

namespace CampusFix.Api.Services;
public partial class CampusService
{
    internal static HashSet<string> ResourceAreas(JsonObject resource,List<JsonObject> config)
    {
        if(resource.S("kind")=="areas")return [resource.S("id")];
        if(resource.S("serviceId")!=""){
            var service=config.FirstOrDefault(c=>c.S("kind")=="servicios"&&c.S("id")==resource.S("serviceId"));
            return service==null?[]:ResourceAreas(service,config);
        }
        if(resource.S("kind")=="categorias")return [];
        var areas=resource.A("sharedAreaIds").Select(x=>x!.ToString()).ToHashSet();
        if(resource.S("responsibleAreaId")!="")areas.Add(resource.S("responsibleAreaId"));
        return areas;
    }
    bool CanEditResource(JsonObject resource,List<JsonObject> config)
    {
        if(u.S("role")=="Superadministrador")return true;
        if(u.S("role")!="Administrador"||!p.B("gestionarConfiguracion")||resource.S("kind") is "organizacion" or "sedes" or "ubicaciones")return false;
        var areas=ResourceAreas(resource,config);
        // A shared record is editable only by an admin responsible for every owning area.
        return areas.Count>0&&areas.All(a=>p.A("managedAreas").Has(a));
    }
    List<JsonObject> ScopedConfig(List<JsonObject> config)
    {
        return config.Where(c=>u.S("role")!="Administrador"||c.S("kind") is "organizacion" or "areas" or "sedes" or "ubicaciones"||ResourceAreas(c,config).Count==0||ResourceAreas(c,config).Any(a=>p.A("managedAreas").Has(a))).Select(c=>{
            var result=c.Copy();result["canEdit"]=CanEditResource(c,config);result["scopeAreaIds"]=new JsonArray(ResourceAreas(c,config).Select(a=>(JsonNode?)JsonValue.Create(a)).ToArray());return result;
        }).ToList();
    }
    internal static bool EligibleForArea(JsonObject person,string area,List<JsonObject> config)
        =>area!=""&&!person.B("eliminado")&&person.B("activo")&&person.S("role")=="Técnico"&&(person.A("operationalAreas").Has(area)||config.Any(c=>c.S("kind")=="equipos"&&c.B("activo")&&ResourceAreas(c,config).Contains(area)&&c.A("members").Has(person.S("id"))));

    void ValidateResourceScope(JsonObject body,JsonObject? old,List<JsonObject> config,List<JsonObject> users)
    {
        Require(old==null||CanEditResource(old,config),403,"No puedes modificar este recurso de otra área o de alcance global/compartido.");
        Require(CanEditResource(body,config),403,"Selecciona un área que administras; los recursos globales requieren Superusuario.");
        if(u.S("role")!="Superadministrador"&&body.S("kind")=="areas"){
            Require(old!=null,403,"Solo el Superusuario crea áreas.");
            foreach(var key in new[]{"managers","parentId","activo","sharedAreaIds"})Require(JsonNode.DeepEquals(body[key]??(key is "managers" or "sharedAreaIds"?new JsonArray():null),old![key]??(key is "managers" or "sharedAreaIds"?new JsonArray():null)),403,"No puedes modificar responsables, jerarquía ni acceso del área.");
        }
        var scope=ResourceAreas(body,config);
        Require(body.A("sharedAreaIds").All(a=>config.Any(c=>c.S("kind")=="areas"&&c.B("activo")&&c.S("id")==a!.ToString())),400,"Área compartida no válida.");
        foreach(var key in new[]{"teamId","calendarId"})if(body.S(key)!=""){
            var reference=config.First(c=>c.S("id")==body.S(key));var target=ResourceAreas(reference,config);
            Require(target.Count==0||scope.Count>0&&scope.All(target.Contains),400,"El recurso sugerido no pertenece a las áreas del servicio.");
        }
        if(body.S("kind")=="equipos"&&scope.Count>0){
            foreach(var member in body.A("members")){
                var person=users.First(x=>x.S("id")==member!.ToString());
                // Existing membership in this team is not evidence for granting new areas.
                Require(scope.Any(a=>EligibleForArea(person,a,config.Where(c=>c.S("id")!=body.S("id")).ToList())||old!=null&&ResourceAreas(old,config).Contains(a)&&old.A("members").Has(person.S("id"))),400,"El responsable no tiene un área de atención compatible con el equipo.");
            }
            Require(body.A("managers").All(m=>users.Any(x=>x.S("id")==m!.ToString()&&x.S("role")=="Administrador")&&scope.Any(a=>config.First(c=>c.S("id")==a).A("managers").Has(m!.ToString()))),400,"El administrador del equipo debe estar vinculado a su área.");
        }
    }
}
