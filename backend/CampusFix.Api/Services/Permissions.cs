using System.Text.Json.Nodes;
using CampusFix.Api.Data;
using static CampusFix.Api.Services.J;
namespace CampusFix.Api.Services;
public static class Permissions {
 public static readonly string[] Roles=["Superadministrador","Administrador","Técnico","Solicitante"];
 public static readonly string[] Keys=["panel","tablero","conocimiento","analitica","usuarios","configuracion","crear","editar","asignar","avanzar","solucion","archivar","internos","gestionarUsuarios","gestionarConfiguracion","gestionarConocimiento"];
 public static bool Admin(JsonObject u)=>u.S("role") is "Administrador" or "Superadministrador";
 public static JsonObject Defaults(string role){var p=new JsonObject();foreach(var k in Keys)p[k]=role is "Administrador" or "Superadministrador"||new[]{"panel","tablero","conocimiento","crear"}.Contains(k)||role=="Técnico"&&new[]{"avanzar","solucion","internos","gestionarConocimiento"}.Contains(k);p["scope"]=role is "Administrador" or "Superadministrador"?"todos":role=="Técnico"?"asignados":"propios";return p;}
 public static async Task<JsonObject> Effective(IStore s,JsonObject u){if(u.S("role")=="Superadministrador")return Defaults(u.S("role"));var role=await s.Get("roles",u.S("role"));var p=Merge(Merge(Defaults(u.S("role")),role.O("permissions")),u.O("permissions"));p["scope"]=u.S("role")=="Administrador"?"areas":u.S("role")=="Técnico"?"asignados":"propios";p["managedAreas"]=new JsonArray((await s.All("config")).Where(c=>c.S("kind")=="areas"&&c.A("managers").Has(u.S("id"))).Select(c=>(JsonNode?)JsonValue.Create(c.S("id"))).ToArray());if(u.S("role")=="Solicitante"){p["conocimiento"]=false;p["gestionarConocimiento"]=false;p["internos"]=false;}return p;}
 public static JsonObject Public(JsonObject u)=>Obj(new {version=u.I("_v"),id=u.S("id"),nombre=u.S("nombre"),correo=u.S("correo"),role=u.S("role"),roles=new[]{u.S("role")},activo=u.B("activo"),mustChange=u.B("mustChange"),needsProfile=u.S("role")=="Solicitante"&&u.S("areaId")=="",areaId=u.S("areaId"),areas=u.A("areas").DeepClone(),operationalAreas=u.A("operationalAreas").DeepClone(),permissions=u.O("permissions").DeepClone(),scope=u.S("scope")});
 public static bool See(JsonObject u,JsonObject p,JsonObject t)=>u.S("role") switch{"Superadministrador"=>true,"Administrador"=>p.A("managedAreas").Has(t.S("responsibleAreaId")),"Técnico"=>t.S("tecnicoId")==u.S("id")||t.S("reportadoPorId")==u.S("id"),_=>t.S("reportadoPorId")==u.S("id")};
 public static bool Attend(JsonObject u,JsonObject p,JsonObject t)=>Admin(u)&&See(u,p,t)||u.S("role")=="Técnico"&&t.S("tecnicoId")==u.S("id");
 public static bool ReadArticle(JsonObject a,JsonObject p)=>(a.S("visibility")!="interno"||p.B("internos"))&&(p.B("gestionarConocimiento")||p.B("conocimiento")&&a.S("status")=="Publicado");
 public static JsonObject SafeTicket(JsonObject t,JsonObject p){var j=t.Copy();j["historial"]=Arr(t.A("historial").Items().Where(h=>!h.B("internal")||p.B("internos")));j["messages"]=Arr(t.A("messages").Items().Where(h=>!h.B("internal")||p.B("internos")));var custom=t.O("custom").Copy();foreach(var k in custom.Select(x=>x.Key).ToArray())if(!p.B("internos")&&t.A("internalFields").Has(k))custom.Remove(k);j["custom"]=custom;return j;}
}
