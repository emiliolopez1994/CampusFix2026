using System.Net.Http.Json;
using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;
namespace CampusFix.Api.Services;

public partial class CampusService {
    async Task<object> AcademicContinuity(string method,JsonObject body){
        Need("analitica");Require(Admin(u),403,"Continuidad académica requiere administración de área o Superusuario.");
        var key=configuration["Continuity:ApiKey"];
        Require(!string.IsNullOrWhiteSpace(key),503,"El servicio de continuidad académica no está iniciado.");
        var areas=(await ActiveRecords("config")).Where(c=>c.S("kind")=="areas"&&c.B("activo")&&(u.S("role")=="Superadministrador"||p.A("managedAreas").Has(c.S("id")))).Select(c=>new{id=c.S("id"),name=c.S("nombre")}).ToArray();
        var ids=areas.Select(a=>a.id).ToHashSet();
        var tickets=(await s.All("tickets")).Where(t=>!t.B("eliminado")&&t.I("estado")!=4&&ids.Contains(t.S("responsibleAreaId"))&&See(u,p,t)).Select(t=>new{id=t.S("id"),areaId=t.S("responsibleAreaId"),subject=t.S("problema"),priority=t.I("prioridad")}).ToArray();
        var payload=new{scope=new{actor=u.S("id"),areas=areas.Select(a=>a.id),tickets},command=method=="POST"?body:null};
        try{
            using var request=new HttpRequestMessage(HttpMethod.Post,"http://127.0.0.1:5149/v1/"+(method=="POST"?"command":"view"));
            request.Headers.Add("X-Continuity-Key",key);request.Content=JsonContent.Create(payload);
            using var response=await clients.CreateClient("continuity").SendAsync(request);
            var json=await response.Content.ReadFromJsonAsync<JsonObject>();
            if(!response.IsSuccessStatusCode){var status=(int)response.StatusCode;throw new ApiError(status is 400 or 403 or 404 or 409?status:503,json?.S("title")??"Continuidad académica no está disponible temporalmente.");}
            Require(json!=null,503,"No se pudo cargar continuidad académica.");
            json!["areas"]=System.Text.Json.JsonSerializer.SerializeToNode(areas);json["tickets"]=System.Text.Json.JsonSerializer.SerializeToNode(tickets);return json;
        }catch(Exception e) when(e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException){throw new ApiError(503,"No se pudo conectar con continuidad académica. Actualiza antes de reintentar una operación; los demás módulos siguen disponibles.");}
    }
}
