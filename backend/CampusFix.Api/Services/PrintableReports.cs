using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CampusFix.Api.Models;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;
namespace CampusFix.Api.Services;

public partial class CampusService {
    async Task<object> PrintableReport(JsonObject body) {
        Need("analitica");
        var key = configuration["Reports:ApiKey"];
        Require(!string.IsNullOrWhiteSpace(key),503,"El servicio de informes imprimibles no está disponible. Puedes utilizar las descargas CSV.");
        Require(body["ids"] is JsonArray,400,"Selecciona las solicitudes del informe.");
        var values = body.A("ids");
        Require(values.Count <= 5000 && values.All(v => v is JsonValue j && j.TryGetValue<string>(out _)),400,"Selecciona hasta 5000 solicitudes.");
        var ids = values.Select(v => v!.GetValue<string>()).ToHashSet();
        var tickets = (await RoutedTickets()).Where(t => !t.B("eliminado") && See(u,p,t) && ids.Contains(t.S("id"))).ToList();
        Require(tickets.Count == ids.Count,409,"La selección cambió o contiene solicitudes no accesibles. Actualiza el listado.");
        var areas = (await ActiveRecords("config")).Where(c=>c.S("kind")=="areas").ToDictionary(c=>c.S("id"),c=>c.S("nombre"));
        var payload = new { tickets = tickets.Select(t => new {
            id=t.S("id"),subject=t.S("problema"),area=areas.GetValueOrDefault(t.S("responsibleAreaId"),"Sin clasificar"),
            service=t.S("serviceNombre","Ayuda general"),assignee=t.S("tecnicoNombre","Sin responsable asignado"),state=t.I("estado"),priority=t.I("prioridad")
        }) };
        try {
            using var request = new HttpRequestMessage(HttpMethod.Post,"http://127.0.0.1:5148/v1/reports/printable");
            request.Headers.Add("X-Reports-Key",key);
            request.Content = JsonContent.Create(payload);
            using var response = await clients.CreateClient("reports").SendAsync(request);
            Require(response.IsSuccessStatusCode,503,"El servicio de informes imprimibles no está disponible. Puedes utilizar las descargas CSV.");
            return new Archivo { Name="CampusFix-solicitudes.html",Type="text/html; charset=utf-8",Content=await response.Content.ReadAsByteArrayAsync() };
        } catch (Exception e) when (e is HttpRequestException or TaskCanceledException) {
            throw new ApiError(503,"El servicio de informes imprimibles no está disponible. Puedes utilizar las descargas CSV.");
        }
    }
}
