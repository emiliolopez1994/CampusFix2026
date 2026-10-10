using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;

namespace CampusFix.Api.Services;
public partial class CampusService
{
    JsonObject RouteTicket(JsonObject ticket,List<JsonObject> config,List<JsonObject> users)
    {
        // Legacy records acquire only explicit service metadata, never requester membership.
        if(!ticket.ContainsKey("responsibleAreaId")){
            var service=config.FirstOrDefault(c=>c.S("id")==ticket.S("serviceId")&&c.S("kind")=="servicios");
            ticket["responsibleAreaId"]=service.S("responsibleAreaId");
            ticket["responsibleAreaName"]=config.FirstOrDefault(c=>c.S("id")==service.S("responsibleAreaId")&&c.S("kind")=="areas").S("nombre");
        }
        var area=config.FirstOrDefault(c=>c.S("id")==ticket.S("responsibleAreaId")&&c.S("kind")=="areas");
        ticket["needsClassification"]=area==null||!area.B("activo")||!users.Any(person=>person.B("activo")&&person.S("role")=="Administrador"&&area.A("managers").Has(person.S("id")));
        ticket["participants"]=Arr(users.Where(person=>person.B("activo")&&(person.S("id")==ticket.S("reportadoPorId")||person.S("id")==ticket.S("tecnicoId")||person.S("role")=="Administrador"&&area.A("managers").Has(person.S("id")))).Select(person=>Obj(new{id=person.S("id"),nombre=person.S("nombre"),role=person.S("role")})));
        return ticket;
    }
    async Task<List<JsonObject>> RoutedTickets(){var configs=await s.All("config");var users=await s.All("users");return (await s.All("tickets")).Select(t=>RouteTicket(t,configs,users)).ToList();}
    async Task<JsonObject?> RoutedTicket(string id){var t=await s.Get("tickets",id);return t==null?null:RouteTicket(t,await s.All("config"),await s.All("users"));}
    bool ManagePerson(JsonObject person)=>u.S("role")=="Superadministrador"||u.S("role")=="Administrador"&&person.S("role") is "Solicitante" or "Técnico"&&(person.A("operationalAreas").Any(a=>p.A("managedAreas").Has(a!.ToString()))||person.S("role")=="Solicitante"&&person.A("areas").Any(a=>p.A("managedAreas").Has(a!.ToString())));
    async Task<JsonArray> Directory(List<JsonObject> tickets)
    {
        var result=new List<JsonObject>();var all=await s.All("users");var config=await s.All("config");var workload=Admin(u)?await s.All("tickets"):tickets;
        foreach(var person in all){
            var canManage=p.B("gestionarUsuarios")&&ManagePerson(person);
            var candidate=Admin(u)&&p.B("asignar")&&person.S("role")=="Técnico"&&person.B("activo");
            if(person.S("id")!=u.S("id")&&!canManage&&!candidate)continue;
            var dto=canManage||person.S("id")==u.S("id")?Public(person):Obj(new{id=person.S("id"),nombre=person.S("nombre"),role=person.S("role"),activo=person.B("activo"),areaId=person.S("areaId"),operationalAreas=person.A("operationalAreas").DeepClone()});
            dto["canManage"]=canManage;
            var effective=await Effective(s,person);dto["eligibleTechnician"]=person.B("activo")&&person.S("role")=="Técnico"&&effective.B("avanzar")&&effective.B("solucion");
            dto["openTickets"]=workload.Count(t=>!t.B("eliminado")&&t.I("estado")!=4&&t.S("tecnicoId")==person.S("id"));
            dto["teams"]=new JsonArray(config.Where(c=>c.S("kind")=="equipos"&&c.A("members").Has(person.S("id"))).Select(c=>(JsonNode?)JsonValue.Create(c.S("nombre"))).ToArray());
            result.Add(dto);
        }
        return Arr(result);
    }
    async Task<object> ClassifyTicket(string id,JsonObject b)
    {
        Owner();var t=await RoutedTicket(id);Require(t!=null&&!t.B("eliminado"),404,"Reporte no encontrado.");
        Require(b.ContainsKey("version")&&b.I("version",-1)==t.I("_v"),409,"El reporte cambió. Actualiza antes de clasificar.");
        var service=await s.Get("config",Required(b,"serviceId","Servicio"));Require(service!=null&&service.S("kind")=="servicios"&&service.B("activo"),400,"Servicio no válido.");
        var area=await s.Get("config",service.S("responsibleAreaId"));Require(area!=null&&area.S("kind")=="areas"&&area.B("activo"),400,"Configura primero el área responsable del servicio.");
        var before=t!.S("responsibleAreaId");t["serviceId"]=service!.S("id");t["serviceNombre"]=service.S("nombre");t["responsibleAreaId"]=area!.S("id");t["responsibleAreaName"]=area.S("nombre");t.Remove("_routingBackfillV1");
        t["fechaActualizacion"]=Now();t.A("historial").Add(Merge(Event("Área responsable clasificada"),Obj(new{previousAreaId=before,responsibleAreaId=area.S("id"),actorId=u.S("id")})));
        t=await s.Save("tickets",t);await Audit("Clasificación de reporte",id);await NotifyEvent(t,"classified",$"Reporte CF-{id}: asignado a tu área responsable");return SafeTicket(RouteTicket(t,await s.All("config"),await s.All("users")),p);
    }
}
