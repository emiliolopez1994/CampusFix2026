using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;
namespace CampusFix.Api.Services;
public partial class CampusService
{
    async Task<List<JsonObject>> ActiveRecords(string kind)=>(await s.All(kind)).Where(x=>!x.B("eliminado")).ToList();
    static void DeleteVersion(JsonObject item,JsonObject body,IQueryCollection query){
        var version=query["version"].ToString();
        Require(version!=""||body.ContainsKey("version"),409,"Actualiza el registro antes de eliminarlo.");
        Require((version!=""?int.TryParse(version,out var n)?n:-1:body.I("version",-1))==item.I("_v"),409,"El registro cambió. Actualiza antes de eliminarlo.");
    }
    void MarkDeleted(JsonObject item){item["eliminado"]=true;item["fechaEliminacion"]=Now();item["eliminadoPorId"]=u.S("id");}
    static bool References(JsonNode? node,string id){
        if(node is JsonObject obj)return obj.Any(pair=>
            (pair.Key is "areaId" or "responsibleAreaId" or "serviceId" or "categoryId" or "teamId" or "sedeId" or "calendarId" or "parentId" or "reportadoPorId" or "tecnicoId")&&pair.Value?.ToString()==id||
            (pair.Key is "areas" or "operationalAreas" or "sharedAreaIds" or "members" or "managers")&&pair.Value is JsonArray a&&a.Any(v=>v?.ToString()==id));
        return false;
    }
    async Task RequireNoDependencies(string id){
        var dependencies=new List<string>();
        foreach(var kind in new[]{"config","users","tickets","articles"}){
            var count=(await ActiveRecords(kind)).Count(x=>x.S("id")!=id&&
                (kind=="tickets"?x.I("estado")<4:kind=="articles"?x.S("status")!="Archivado":x.B("activo",true))&&References(x,id));
            if(count>0)dependencies.Add($"{(kind switch{"config"=>"recursos","users"=>"usuarios","tickets"=>"solicitudes abiertas",_=>"artículos"})}: {count}");
        }
        Require(dependencies.Count==0,409,"El registro tiene dependencias activas ("+string.Join(", ",dependencies)+"). Resuélvelas antes de retirarlo; no se reasignó ni eliminó información relacionada.");
    }
    async Task<object> DeleteRecord(string kind,string id,JsonObject body,IQueryCollection query){
        var item=await s.Get(kind,id);Require(item!=null&&!item.B("eliminado"),404,"Registro no encontrado.");
        if(kind=="config"){
            Need("gestionarConfiguracion");Require(Admin(u),403,"Acción reservada a administración.");
            Require(item.S("kind")!="organizacion",403,"La organización es un registro esencial y no se puede eliminar.");
            Require(CanEditResource(item,await ActiveRecords("config"))&&(item.S("kind")!="areas"||u.S("role")=="Superadministrador"),403,"No puedes eliminar este recurso fuera de tu ámbito.");
        }else if(kind=="users"){
            Need("gestionarUsuarios");Require(Admin(u)&&ManagePerson(item),403,"No puedes administrar esta cuenta.");
            Require(id!=u.S("id"),403,"No puedes eliminar tu propia cuenta.");
            if(u.S("role")=="Administrador"){
                var config=await ActiveRecords("config");
                var areas=item.S("role")=="Técnico"?config.Where(c=>c.S("kind")=="areas"&&EligibleForArea(item,c.S("id"),config)).Select(c=>c.S("id")).ToList():item.A("areas").Select(a=>a!.ToString()).ToList();
                Require(areas.Count>0&&areas.All(a=>p.A("managedAreas").Has(a)),403,"La cuenta también pertenece a otras áreas; debe retirarla el Superadministrador.");
            }
            if(item.S("role")=="Superadministrador"){
                Owner();Require((await ActiveRecords("users")).Count(x=>x.S("role")=="Superadministrador"&&x.B("activo")&&x.S("id")!=id)>0,409,"No puedes eliminar el último Superadministrador activo.");
            }
        }else if(kind=="articles"){
            Need("gestionarConocimiento");Require(Admin(u)&&await CanReadKnowledge(item),403,"No puedes eliminar este artículo.");
        }else throw new ApiError(404,"Ruta no encontrada.");
        DeleteVersion(item,body,query);
        if(kind is "config" or "users")await RequireNoDependencies(id);
        MarkDeleted(item);if(kind is "config" or "users")item["activo"]=false;
        // Existing password hashes and historical references remain untouched.
        await s.Save(kind,item);await Audit("Registro eliminado lógicamente",kind+"/"+id);return new{ok=true};
    }
    async Task<object> PermissionProfileApi(string method,JsonObject body,IQueryCollection query){
        Owner();if(method=="GET")return await ActiveRecords("permissionProfiles");
        var id=body.S("id",query["id"].ToString());var old=id==""?null:await s.Get("permissionProfiles",id);
        Require(id==""||old!=null&&!old.B("eliminado"),404,"Perfil no encontrado.");
        if(method=="DELETE"){Require(old!=null,404,"Perfil no encontrado.");DeleteVersion(old,body,query);MarkDeleted(old);old["activo"]=false;await s.Save("permissionProfiles",old);}
        else{
            Require(method is "POST" or "PUT",405,"Operación no permitida.");Version(old,body);
            var name=Required(body,"nombre","Nombre",100);Require(!(await ActiveRecords("permissionProfiles")).Any(x=>x.S("id")!=id&&string.Equals(x.S("nombre").Trim(),name,StringComparison.OrdinalIgnoreCase)),409,"Ya existe un perfil con ese nombre.");
            Require(Roles.Contains(body.S("baseRole"))&&body.S("baseRole")!="Superadministrador",400,"Selecciona un rol base no protegido.");
            var permissions=new JsonObject();foreach(var key in Keys)if(body.O("permissions")[key] is JsonValue value&&value.TryGetValue<bool>(out var enabled))permissions[key]=enabled;
            await s.Save("permissionProfiles",Merge(old??new(),Obj(new{id=id==""?Id():id,nombre=name,baseRole=body.S("baseRole"),activo=body.B("activo",true),permissions})));
        }
        await Audit("Perfil de permisos actualizado",id);return new{ok=true};
    }
    async Task<object> EditOwnMessage(string ticketId,string messageId,JsonObject body){
        var ticket=await RoutedTicket(ticketId);Require(ticket!=null&&!ticket.B("eliminado")&&See(u,p,ticket),404,"Solicitud no encontrada.");
        Require(u.S("role")=="Técnico",403,"Esta acción corresponde al técnico autor del mensaje.");
        Require(body.ContainsKey("version"),409,"Actualiza la conversación antes de editar.");Version(ticket,body);
        var message=ticket.A("messages").Items().FirstOrDefault(m=>m.S("id")==messageId);
        Require(message!=null&&message.S("userId")==u.S("id"),403,"Solo puedes editar tus propios mensajes.");
        Require(!message.B("internal")||p.B("internos"),403,"No tienes acceso al mensaje interno.");
        message["text"]=Required(body,"text","Mensaje",4000);message["editedAt"]=Now();
        ticket["fechaActualizacion"]=Now();ticket=await s.Save("tickets",ticket);await Audit("Mensaje propio editado",ticketId+"/"+messageId);return SafeTicket(ticket,p);
    }
}
