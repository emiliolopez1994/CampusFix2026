using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;

namespace CampusFix.Api.Services;
public partial class CampusService
{
    async Task NotifyEvent(JsonObject ticket,string type,string title,string targetId="",bool internalOnly=false)
    {
        var users=await ActiveRecords("users");var config=await ActiveRecords("config");var mail=await s.Get("settings","mail");
        var t=RouteTicket(ticket,config,users);
        foreach(var person in users.Where(x=>x.B("activo"))){
            var id=person.S("id");var perms=await Effective(s,person);
            var manager=person.S("role")=="Administrador"&&See(person,perms,t);
            var owner=person.S("role")=="Superadministrador";
            var recipient=type switch {
                "created"=>id==t.S("reportadoPorId")||manager||owner&&t.B("needsClassification"),
                "classified"=>manager,
                "assigned"=>id==t.S("reportadoPorId")||id==t.S("tecnicoId"),
                "resolved"=>id==t.S("reportadoPorId")||manager,
                "action"=>id==targetId,
                "sla"=>manager||owner&&t.I("prioridad")==4&&t.B("needsClassification"),
                _=>false
            };
            if(!recipient||!See(person,perms,t)||internalOnly&&!perms.B("internos")||type is not "created" and not "sla"&&id==u.S("id"))continue;
            var eventKey=type=="sla"?$"ticket-{t.S("id")}-sla":$"ticket-{t.S("id")}-v{t.I("_v")}-{type}";
            var noticeId=eventKey+"-"+id;
            var added=await s.CreateOnce("notifications",Obj(new{id=noticeId,userId=id,title,ticketId=t.S("id"),eventType=type,eventKey,@internal=internalOnly,fecha=Now(),read=false}));
            if(added&&mail.B("enabled")&&!configuration.GetValue<bool>("LocalProvisioning:SuppressMail"))await s.CreateOnce("mail",Obj(new{id=noticeId,userId=id,to=person.S("correo"),title,ticketId=t.S("id"),status="pending",attempts=0,created=Now(),next=Now()}));
        }
    }
    async Task EnsureSlaNotices(List<JsonObject> tickets)
    {
        // Only the responsible admin's polling evaluates that area's reminders.
        if(!Admin(u))return;
        foreach(var t in tickets.Where(t=>!t.B("eliminado")&&t.I("estado")<4&&See(u,p,t))){
            var meta=t.O("meta");
            var overdue=meta.S("resolutionDue")!=""&&Date(meta.S("resolutionDue"))<DateTimeOffset.UtcNow||t.S("firstResponseAt")==""&&meta.S("responseDue")!=""&&Date(meta.S("responseDue"))<DateTimeOffset.UtcNow;
            var waiting=t.S("tecnicoId")==""&&Date(t.S("fechaReporte"))<DateTimeOffset.UtcNow.AddHours(-24);
            if(overdue||waiting)await NotifyEvent(t,"sla",$"CF-{t.S("id").PadLeft(4,'0')}: requiere atención por {(overdue?"meta vencida":"asignación pendiente")}");
        }
    }
    async Task ValidateActionTarget(JsonObject t,string target,bool internalOnly)
    {
        Require(target!=""&&target!=u.S("id"),400,"Elige un destinatario distinto del autor para solicitar una acción.");
        var person=await s.Get("users",target);Require(person!=null&&person.B("activo"),400,"Destinatario no válido.");
        var perms=await Effective(s,person!);
        Require(See(person!,perms,t)&&(target==t.S("reportadoPorId")||target==t.S("tecnicoId")||person!.S("role")=="Administrador")&&(!internalOnly||perms.B("internos")),403,"El destinatario no participa en este reporte o no puede leer notas internas.");
    }
}
