using System.Net;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5148");
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2_000_000);
var key = builder.Configuration["Reports:ApiKey"];
if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
    throw new InvalidOperationException("Configura Reports:ApiKey con al menos 32 caracteres.");
var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
var app = builder.Build();
app.Use(async (context, next) => {
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    if (context.Request.Path != "/health" && !CryptographicOperations.FixedTimeEquals(
        keyHash, SHA256.HashData(Encoding.UTF8.GetBytes(context.Request.Headers["X-Reports-Key"].ToString())))) {
        context.Response.StatusCode = 401; return;
    }
    await next();
});
app.MapGet("/health", () => new { service = "CampusFix.Reports", status = "ready", version = 1 });
app.MapPost("/v1/reports/printable", (ReportRequest input) => {
    if (input.Tickets is null || input.Tickets.Length > 5000 || input.Tickets.Any(t => t is null ||
        t.State is < 1 or > 4 || t.Priority is < 1 or > 4 ||
        new[] {t.Id,t.Subject,t.Area,t.Service,t.Assignee}.Any(v => v is null || v.Length > 1000)))
        return Results.BadRequest(new { title = "Informe no válido; máximo 5000 solicitudes." });
    string E(string? value) => WebUtility.HtmlEncode(value ?? "");
    string[] states = ["", "Reportado", "Asignado", "En proceso", "Solucionado"];
    string[] priorities = ["", "Baja", "Media", "Alta", "Crítica"];
    var html = new StringBuilder("""
        <!doctype html><html lang="es"><head><meta charset="utf-8">
        <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'">
        <meta name="viewport" content="width=device-width, initial-scale=1"><title>CampusFix · Informe de solicitudes</title>
        <style>body{font:14px system-ui,sans-serif;color:#172033;margin:32px}h1{color:#164ea6}table{border-collapse:collapse;width:100%;font-size:12px}th,td{padding:10px;border:1px solid #ccd3df;text-align:left;overflow-wrap:anywhere}th{background:#edf3ff}small{color:#526079}thead{display:table-header-group}tr{break-inside:avoid}@page{size:A4 landscape;margin:15mm}@media print{body{margin:0}.hint{display:none}}</style></head><body>
        <h1>CampusFix · Informe de solicitudes</h1>
        """);
    html.Append($"<p>Generado: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC · {input.Tickets.Length} solicitudes seleccionadas · {input.Tickets.Count(t => t.State == 4)} solucionadas</p>");
    html.Append("<p class=\"hint\">Usa Imprimir en tu navegador para imprimir este informe o guardarlo como PDF. Contiene los registros seleccionados y autorizados al generar la descarga.</p>");
    html.Append("<table><thead><tr><th>Solicitud</th><th>Problema</th><th>Área responsable</th><th>Servicio</th><th>Responsable asignado</th><th>Estado</th><th>Prioridad</th></tr></thead><tbody>");
    foreach (var t in input.Tickets)
        html.Append($"<tr><td>CF-{E(t.Id.PadLeft(4,'0'))}</td><td>{E(t.Subject)}</td><td>{E(t.Area)}</td><td>{E(t.Service)}</td><td>{E(t.Assignee)}</td><td>{states[t.State]}</td><td>{priorities[t.Priority]}</td></tr>");
    html.Append("</tbody></table><p><small>Documento de consulta. Los registros pueden cambiar después de esta descarga.</small></p></body></html>");
    return Results.Text(html.ToString(), "text/html", Encoding.UTF8);
});
app.Run();

record ReportRequest(ReportTicket[] Tickets);
record ReportTicket(string Id, string Subject, string Area, string Service, string Assignee, int State, int Priority);
