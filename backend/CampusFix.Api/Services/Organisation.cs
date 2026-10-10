using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;

namespace CampusFix.Api.Services;
public partial class CampusService
{
    async Task<JsonObject> Membership(JsonObject body)
    {
        var areas=(await s.All("config")).Where(c=>c.S("kind")=="areas"&&c.B("activo")).ToList();var id=body.S("areaId");
        if(areas.Count>0&&!areas.Any(a=>a.S("id")==id)||areas.Count==0&&id!="")throw new ApiError(400,"Selecciona un área de pertenencia activa.","areaId");
        return Obj(new{areaId=id,areas=id==""?Array.Empty<string>():new[]{id}});
    }
    async Task<object> UpdateMembership(JsonObject body)
    {
        Require(u.S("role")=="Solicitante",403,"La pertenencia del personal se gestiona desde administración.");
        Require(body.ContainsKey("version")&&body.I("version",-1)==u.I("_v"),409,"Tu perfil cambió. Actualiza antes de guardar.");
        u=await s.Save("users",Merge(u,await Membership(body)));await Audit("Área de pertenencia actualizada por solicitante",u.S("id"));var result=Public(u);result["permissions"]=await Effective(s,u);return result;
    }
    JsonObject RouteTicket(JsonObject ticket,List<JsonObject> config,List<JsonObject> users)
    {
        // Legacy snapshots are backfilled only by the explicit migration, never by reads.
        if(!ticket.ContainsKey("responsibleAreaId")){
            ticket["responsibleAreaId"]="";
            ticket["responsibleAreaName"]="";
        }
        var area=config.FirstOrDefault(c=>c.S("id")==ticket.S("responsibleAreaId")&&c.S("kind")=="areas");
        ticket["needsClassification"]=area==null||!area.B("activo")||!users.Any(person=>person.B("activo")&&person.S("role")=="Administrador"&&area.A("managers").Has(person.S("id")));
        ticket["classificationReason"]=area==null?"Selecciona un servicio con área responsable.":!area.B("activo")?"El área responsable está inactiva.":ticket.B("needsClassification")?"El área no tiene un administrador activo. Configura sus responsables antes de clasificar.":"";
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
            var candidate=Admin(u)&&p.B("asignar")&&person.S("role")=="Técnico"&&person.B("activo")&&(u.S("role")=="Superadministrador"||p.A("managedAreas").Any(a=>EligibleForArea(person,a!.ToString(),config)));
            var coManager=Admin(u)&&person.S("role")=="Administrador"&&config.Any(c=>c.S("kind")=="areas"&&c.A("managers").Has(person.S("id"))&&(u.S("role")=="Superadministrador"||p.A("managedAreas").Has(c.S("id"))));
            if(person.S("id")!=u.S("id")&&!canManage&&!candidate&&!coManager)continue;
            var dto=canManage||person.S("id")==u.S("id")?Public(person):Obj(new{id=person.S("id"),nombre=person.S("nombre"),role=person.S("role"),activo=person.B("activo"),areaId=person.S("areaId"),operationalAreas=person.A("operationalAreas").DeepClone()});
            dto["canManage"]=canManage;dto["eligibleAreaIds"]=new JsonArray(config.Where(c=>c.S("kind")=="areas"&&EligibleForArea(person,c.S("id"),config)).Select(c=>(JsonNode?)JsonValue.Create(c.S("id"))).ToArray());
            var effective=await Effective(s,person);dto["eligibleTechnician"]=person.B("activo")&&person.S("role")=="Técnico"&&effective.B("avanzar")&&effective.B("solucion");
            dto["openTickets"]=workload.Count(t=>!t.B("eliminado")&&t.I("estado")!=4&&t.S("tecnicoId")==person.S("id"));
            dto["teams"]=new JsonArray(config.Where(c=>c.S("kind")=="equipos"&&c.A("members").Has(person.S("id"))).Select(c=>(JsonNode?)JsonValue.Create(c.S("nombre"))).ToArray());
            result.Add(dto);
        }
        return Arr(result);
    }
    async Task<object> ClassifyTicket(string id,JsonObject b)
    {
        Need("editar");var t=await RoutedTicket(id);Require(t!=null&&!t.B("eliminado")&&Admin(u)&&See(u,p,t),404,"Reporte no encontrado en tus áreas administradas.");
        Require(b.ContainsKey("version")&&b.I("version",-1)==t.I("_v"),409,"El reporte cambió. Actualiza antes de clasificar.");
        var service=await s.Get("config",Required(b,"serviceId","Servicio"));Require(service!=null&&service.S("kind")=="servicios"&&service.B("activo"),400,"Servicio no válido.");
        var area=await s.Get("config",service.S("responsibleAreaId"));Require(area!=null&&area.S("kind")=="areas"&&area.B("activo"),400,"Configura primero el área responsable del servicio.");
        Require(u.S("role")=="Superadministrador"||p.A("managedAreas").Has(area.S("id")),403,"No administras el área de destino.");
        var people=await s.All("users");Require(people.Any(person=>person.B("activo")&&person.S("role")=="Administrador"&&area.A("managers").Has(person.S("id"))),400,"El área de destino no tiene administrador activo. Configura sus responsables antes de clasificar.");
        var before=t!.S("responsibleAreaId");t["serviceId"]=service!.S("id");t["serviceNombre"]=service.S("nombre");t["responsibleAreaId"]=area!.S("id");t["responsibleAreaName"]=area.S("nombre");t.Remove("_routingBackfillV1");
        t["fechaActualizacion"]=Now();t.A("historial").Add(Merge(Event("Área responsable clasificada"),Obj(new{previousAreaId=before,responsibleAreaId=area.S("id"),actorId=u.S("id")})));
        t=RouteTicket(t,await s.All("config"),people);t=await s.Save("tickets",t);await Audit("Clasificación de reporte",id);await NotifyEvent(t,"classified",$"Reporte CF-{id}: asignado a tu área responsable");return SafeTicket(t,p);
    }
}
