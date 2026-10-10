using CampusFix.Api.Data;
using Microsoft.EntityFrameworkCore;
using CampusFix.Api.Models;
using CampusFix.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static CampusFix.Api.Services.J;
var httpMode=args.Contains("--http");var pg=args.Contains("--postgres")||httpMode;
var testConnection=Environment.GetEnvironmentVariable("CAMPUSFIX_TEST_POSTGRES");
if(pg&&string.IsNullOrWhiteSpace(testConnection))throw new Exception("CAMPUSFIX_TEST_POSTGRES debe apuntar a una base de pruebas vacía.");
if(pg){var c=new Npgsql.NpgsqlConnectionStringBuilder(testConnection);if(c.Host is not "127.0.0.1" and not "localhost"||c.Database?.StartsWith("campusfix_qa",StringComparison.Ordinal)!=true)throw new Exception("Por seguridad QA solo admite localhost y bases cuyo nombre empieza con campusfix_qa.");}
await using var db=pg?new CampusFixDbContext(new DbContextOptionsBuilder<CampusFixDbContext>().UseNpgsql(testConnection!).Options):null;
if(db!=null){await db.Database.MigrateAsync();if(await db.Users.AnyAsync()||await db.Documentos.AnyAsync())throw new Exception("La base de pruebas debe estar vacía. No se modificaron datos.");}
if(db!=null&&!httpMode)await MigrationChecks.Run(db);
if(args.Contains("--seed-browser")){if(db==null)throw new Exception("Browser fixture requires local PostgreSQL.");await BrowserFixture.Seed(new Store(db));return;}
if(args.Contains("--migration-only")){if(db==null)throw new Exception("Migration checks require --postgres.");return;}
await using var transaction=db!=null&&!httpMode?await db.Database.BeginTransactionAsync():null;
IStore store=db==null?new MemoryStore():new Store(db);var hasher=new PasswordHasher<Usuario>();var key=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Jwt:Key",key},{"Mail:EncryptionKey",key}}).Build();int checks=0;
await using var api=httpMode?await QaHttpApi.Start(testConnection!,key):null;
var tokens=new Dictionary<ClaimsPrincipal,string>();
void Assert(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);checks++;Console.WriteLine("PASS "+message);}
async Task<JsonNode> Call(string path,string method="GET",JsonObject? b=null,ClaimsPrincipal? principal=null){if(api!=null)return await api.Call(path,method,b,principal!=null?tokens.GetValueOrDefault(principal):null);var service=new CampusService(store,configuration,hasher,new NoNetwork());return JsonSerializer.SerializeToNode(await service.Run(path,method,b??new(),principal??new(),new QueryCollection()))!;}
async Task Denied(int status,string path,string method,JsonObject b,ClaimsPrincipal principal,string label){try{await Call(path,method,b,principal);throw new Exception("Expected rejection: "+label);}catch(ApiError e){Assert(e.Status==status,label);}}
ClaimsPrincipal Principal(JsonNode response){var principal=new JwtSecurityTokenHandler{MapInboundClaims=false}.ValidateToken(response.S("token"),new(){ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer="CampusFix.Api",ValidAudience="CampusFix.Angular",IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),ClockSkew=TimeSpan.Zero},out _);tokens[principal]=response.S("token");return principal;}
const string password="CampusClaveSegura123#";
foreach(var role in Permissions.Roles){var userId=role switch{"Superadministrador"=>"owner@example.edu","Administrador"=>"admin@example.edu","Técnico"=>"tech@example.edu",_=>"student@example.edu"};await store.Save("users",Obj(new{id=userId,correo=userId,nombre=role,role,activo=true,passwordHash=hasher.HashPassword(new Usuario(),password),stamp=Id(),areas=new[]{"area"},areaId="area",operationalAreas=new[]{"area"}}));}
async Task<ClaimsPrincipal> SignIn(string email)=>Principal(await Call("auth/login","POST",Obj(new{correo=email,password})));
var owner=await SignIn("owner@example.edu");var admin=await SignIn("admin@example.edu");var tech=await SignIn("tech@example.edu");var student=await SignIn("student@example.edu");
Assert((await Call("auth/me",principal:owner)).O("permissions").B("configuracion"),"Identity hash and signed JWT restore owner permissions");
await Denied(401,"data","GET",new(),new(),"Unauthenticated access rejected");
await Denied(403,"permisos","GET",new(),admin,"Administrator cannot edit owner permissions");
await Denied(403,"usuarios","POST",Obj(new{correo="othertech@example.edu",password,nombre="Other",role="Técnico"}),admin,"Only owner creates privileged roles");
await store.Save("config",Obj(new{id="area",kind="areas",nombre="Académica",activo=true,managers=new[]{"admin@example.edu"}}));
await store.Save("config",Obj(new{id="team",kind="equipos",nombre="Soporte",activo=true,members=new[]{"tech@example.edu"}}));
await store.Save("config",Obj(new{id="service",kind="servicios",nombre="Tecnología",activo=true,teamId="team",responsibleAreaId="area",fields=new[]{new{id="asset",label="Activo",type="text",required=true,@internal=false,options=Array.Empty<string>()},new{id="secret",label="Diagnóstico interno",type="text",required=false,@internal=true,options=Array.Empty<string>()}}}));
await store.Save("config",Obj(new{id="meta",kind="metas",nombre="Atención prioritaria",activo=true,prioridad=2,responseHours=4,resolutionHours=24}));
JsonObject TicketBody()=>Obj(new{problema="Proyector sin señal",ubicacion="Aula 205",descripcion="No reconoce HDMI",prioridad=2,areaId="area",serviceId="service",custom=new{asset="P-205",secret="Should be removed"}});
await store.Save("config",Obj(new{id="belonging",kind="areas",nombre="Bienestar",activo=true}));
await store.Save("config",Obj(new{id="other-provider",kind="areas",nombre="Mantenimiento",activo=true,managers=new[]{"other-admin@example.edu"}}));
await store.Save("users",Obj(new{id="other-admin@example.edu",correo="other-admin@example.edu",nombre="Otra administración",role="Administrador",activo=true,passwordHash=hasher.HashPassword(new Usuario(),password),stamp=Id()}));
var otherAdmin=await SignIn("other-admin@example.edu");
var ownTeam=(await Call("config","POST",Obj(new{kind="equipos",nombre="Atención del área",activo=true,responsibleAreaId="area",members=new[]{"tech@example.edu"},managers=new[]{"admin@example.edu"}}),admin)).AsObject();
Assert(ownTeam.S("responsibleAreaId")=="area","Area admin creates own team");
var attempt=ownTeam.Copy();attempt["version"]=ownTeam.I("_v");attempt["nombre"]="Recurso ajeno";
await Denied(403,"config","POST",attempt,otherAdmin,"Foreign admin cannot edit team by ID");
attempt["responsibleAreaId"]="other-provider";await Denied(403,"config","POST",attempt,admin,"Admin cannot move resource into an unauthorized area");
Assert(!(await Call("data",principal:otherAdmin)).A("config").Items().Any(c=>c.S("id")==ownTeam.S("id")),"Scoped resource catalog excludes foreign teams");
var ownService=await Call("config","POST",Obj(new{kind="servicios",nombre="Atención institucional",activo=true,responsibleAreaId="area",teamId=ownTeam.S("id")}),admin);
var ownCategory=await Call("config","POST",Obj(new{kind="categorias",nombre="Consulta institucional",activo=true,serviceId=ownService.S("id"),teamId=ownTeam.S("id")}),admin);
Assert(ownCategory.S("serviceId")==ownService.S("id"),"Admin creates service and category within owned area");
await Denied(403,"config","POST",Obj(new{kind="categorias",nombre="Global",activo=true}),admin,"Admin cannot create global categories");
var wrongDestination=TicketBody();wrongDestination["attentionAreaId"]="other-provider";await Denied(409,"reportes","POST",wrongDestination,student,"Ticket rejects contradictory destination and service");

var crossBody=TicketBody();crossBody["areaId"]="belonging";
var crossUser=(await store.Get("users","student@example.edu"))!;crossUser["areas"]=new JsonArray("area","belonging");await store.Save("users",crossUser);
var crossTicket=await Call("reportes","POST",crossBody,student);
Assert(crossTicket.S("areaId")=="belonging"&&crossTicket.S("responsibleAreaId")=="area","Routing separates requester membership from provider area");
await Denied(404,$"reportes/{crossTicket.S("id")}","GET",new(),otherAdmin,"Other area administrator cannot read ticket");
await Denied(404,$"reportes/{crossTicket.S("id")}/asignar","PUT",Obj(new{tecnicoId="tech@example.edu",version=crossTicket.I("_v")}),otherAdmin,"Other area administrator cannot assign ticket");
Assert(!(await Call("data",principal:otherAdmin)).A("tickets").Items().Any(t=>t.S("id")==crossTicket.S("id")),"Other area dashboard excludes foreign tickets");
await Call($"reportes/{crossTicket.S("id")}","DELETE",new(),admin);
var unknownBody=TicketBody();unknownBody["serviceId"]="";unknownBody["custom"]=new JsonObject();var unknownTicket=await Call("reportes","POST",unknownBody,student);
Assert(unknownTicket.B("needsClassification"),"Missing service routes to owner classification queue");
await Denied(404,$"reportes/{unknownTicket.S("id")}","GET",new(),admin,"Area admin cannot see unclassified ticket");
var classified=await Call($"reportes/{unknownTicket.S("id")}/clasificar","PUT",Obj(new{serviceId="service",version=unknownTicket.I("_v")}),owner);
Assert(classified.S("responsibleAreaId")=="area"&&classified.S("areaId")=="area","Owner classification preserves requester membership");
var reread=await Call($"reportes/{classified.S("id")}",principal:admin);Assert(!reread.B("needsClassification"),"Classification survives subsequent authorized read");
var reclassified=await Call($"reportes/{classified.S("id")}/clasificar","PUT",Obj(new{serviceId="service",version=reread.I("_v")}),admin);Assert(!reclassified.B("needsClassification"),"Authorized area administrator can classify scoped legacy ticket");

await Call($"reportes/{classified.S("id")}","DELETE",new(),admin);
var invalidTicket=TicketBody();invalidTicket["custom"]=new JsonObject();await Denied(400,"reportes","POST",invalidTicket,student,"Required dynamic field rejected with validation error");
await store.Save("config",Obj(new{id="foreign-area",kind="areas",nombre="Área ajena",activo=true}));
invalidTicket=TicketBody();invalidTicket["areaId"]="foreign-area";await Denied(403,"reportes","POST",invalidTicket,student,"Requester cannot create tickets for an unauthorized area");
var unclassified=TicketBody();unclassified["areaId"]="";var unclassifiedTicket=await Call("reportes","POST",unclassified,student);Assert(unclassifiedTicket.S("areaId")=="","Explicit pending classification does not silently restore the main area");
await Call($"reportes/{unclassifiedTicket.S("id")}","DELETE",new(),admin);
var registered=Principal(await Call("auth/registro","POST",Obj(new{correo="new-requester@example.edu",nombre="Nuevo solicitante",password,areaId="belonging"})));
var newBody=TicketBody();newBody["areaId"]="belonging";var newTicket=await Call("reportes","POST",newBody,registered);Assert(newTicket.S("reportadoPorId")=="new-requester@example.edu"&&newTicket.S("areaId")=="belonging","Registration selects membership independently of provider");
var profile=await Call("auth/me",principal:registered);
await Denied(400,"auth/profile","PUT",Obj(new{areaId="missing",version=profile.I("version")}),registered,"Profile rejects unavailable membership");
await Denied(409,"auth/profile","PUT",Obj(new{areaId="area",version=0}),registered,"Profile requires current version");
await Call("auth/profile","PUT",Obj(new{areaId="area",version=profile.I("version")}),registered);
Assert((await Call($"reportes/{newTicket.S("id")}",principal:registered)).S("areaId")=="belonging","Profile correction preserves historical ticket area");
Assert((await Call("auth/me",principal:registered)).S("areaId")=="area","Profile correction applies to future membership");
await Call($"reportes/{newTicket.S("id")}","DELETE",new(),admin);
var reminder=await Call("reportes","POST",TicketBody(),student);var reminderId=reminder.S("id");
var aged=(await store.Get("tickets",reminderId))!;aged["fechaReporte"]=DateTimeOffset.UtcNow.AddDays(-2).ToString("O");await store.Save("tickets",aged);
await Call("data",principal:student);Assert(!(await store.All("notifications")).Any(n=>n.S("ticketId")==reminderId&&n.S("eventType")=="sla"),"Requester polling does not trigger SLA noise");
await Call("data",principal:admin);await Call("data",principal:admin);
var reminders=(await store.All("notifications")).Where(n=>n.S("ticketId")==reminderId&&n.S("eventType")=="sla").ToList();Assert(reminders.Count==1&&reminders[0].S("userId")=="admin@example.edu","Repeated overdue polling notifies responsible admin exactly once");
await Call($"reportes/{reminderId}","DELETE",new(),admin);
var criticalBody=TicketBody();criticalBody["serviceId"]="";criticalBody["custom"]=new JsonObject();criticalBody["prioridad"]=4;var critical=await Call("reportes","POST",criticalBody,student);var criticalId=critical.S("id");
aged=(await store.Get("tickets",criticalId))!;aged["fechaReporte"]=DateTimeOffset.UtcNow.AddDays(-2).ToString("O");await store.Save("tickets",aged);await Call("data",principal:owner);
reminders=(await store.All("notifications")).Where(n=>n.S("ticketId")==criticalId&&n.S("eventType")=="sla").ToList();Assert(reminders.Count==1&&reminders[0].S("userId")=="owner@example.edu","Critical unclassified reminder goes only to owner");await Call($"reportes/{criticalId}","DELETE",new(),owner);
var ticket=(await Call("reportes","POST",TicketBody(),student)).AsObject();var id=ticket.S("id");
Assert(ticket.I("estado")==1&&ticket.S("reportadoPorId")=="student@example.edu"&&ticket.S("teamId")=="team","Create ticket preserves reporter, initial state and suggested team");
var createdNotices=(await store.All("notifications")).Where(n=>n.S("ticketId")==id).ToList();
Assert(createdNotices.Count==2&&createdNotices.All(n=>n.S("userId") is "student@example.edu" or "admin@example.edu"),"Creation notifies only requester and responsible admin");
Assert(ticket["meta"] is JsonObject&&!ticket.O("custom").ContainsKey("secret"),"SLA snapshot recorded; requester cannot inject internal fields");
Assert((await Call("data",principal:tech)).A("tickets").Count==0,"Technician cannot see unassigned tickets outside scope");
await Denied(403,$"reportes/{id}/estado","PATCH",Obj(new{estado=2,version=ticket.I("_v")}),student,"Requester cannot advance ticket state");
await Denied(409,$"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="tech@example.edu",version=0}),admin,"Stale update rejected");
ticket=(await Call($"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="tech@example.edu",version=ticket.I("_v")}),admin)).AsObject();
Assert((await Call("data",principal:tech)).A("tickets").Count==1,"Assigned technician can see ticket");
await store.Save("users",Obj(new{id="foreign-tech@example.edu",correo="foreign-tech@example.edu",nombre="Otro técnico",role="Técnico",activo=true,passwordHash=hasher.HashPassword(new Usuario(),password),stamp=Id()}));var foreignTech=await SignIn("foreign-tech@example.edu");
await Denied(404,$"reportes/{id}","GET",new(),foreignTech,"Unassigned technician cannot access another ticket directly");
foreach(var operation in new[]{("estado","PATCH"),("solucion","PUT"),("mensajes","POST")})await Denied(404,$"reportes/{id}/{operation.Item1}",operation.Item2,Obj(new{estado=2,solucion="Ajena",text="Ajeno",version=ticket.I("_v")}),foreignTech,"Unassigned technician rejected for "+operation.Item1);
await Denied(403,$"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="tech@example.edu",version=ticket.I("_v")}),tech,"Technician cannot change assignment");
Assert(((JsonArray)await Call("notifications",principal:tech)).Items().Any(n=>n.S("ticketId")==id),"Assignment notification visible to assigned technician");
await Denied(400,$"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="foreign-tech@example.edu",version=ticket.I("_v")}),admin,"Unscoped technician cannot be assigned even by administrator");
var authorizedTech=(await store.Get("users","foreign-tech@example.edu"))!;authorizedTech["operationalAreas"]=new JsonArray("area","foreign-area");await store.Save("users",authorizedTech);
ticket=(await Call($"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="foreign-tech@example.edu",version=ticket.I("_v")}),admin)).AsObject();
Assert(ticket.S("tecnicoId")=="foreign-tech@example.edu"&&ticket.A("assignments").Count>=2,"Active technician outside suggested team can be assigned with audit trail");
await Denied(409,$"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="tech@example.edu"}),admin,"Assignment requires explicit concurrency version");
ticket=(await Call($"reportes/{id}/asignar","PUT",Obj(new{tecnicoId="tech@example.edu",version=ticket.I("_v")}),admin)).AsObject();
await Denied(409,$"reportes/{id}/estado","PATCH",Obj(new{estado=3,version=ticket.I("_v")}),tech,"Skipping workflow state rejected");
ticket=(await Call($"reportes/{id}/estado","PATCH",Obj(new{estado=2,version=ticket.I("_v")}),tech)).AsObject();
Assert(!(await store.All("notifications")).Any(n=>n.S("ticketId")==id&&n.S("eventKey").Contains($"-v{ticket.I("_v")}-")),"Intermediate state is persisted without notifications");
var beforeComments=(await store.All("notifications")).Count;
ticket=(await Call($"reportes/{id}/mensajes","POST",Obj(new{text="Revisando conectores",@internal=true,version=ticket.I("_v")}),tech)).AsObject();
Assert(ticket.S("firstResponseAt")=="","Internal note does not count as public response");
var safe=await Call($"reportes/{id}",principal:student);Assert(safe.A("messages").Count==0&&!safe.A("historial").Items().Any(h=>h.B("internal")),"Internal messages and history hidden from requester");
ticket=(await Call($"reportes/{id}/mensajes","POST",Obj(new{text="Recibimos tu solicitud",@internal=false,file=new{name="detalle.txt",type="text/plain",data=Convert.ToBase64String("Conector revisado"u8)},version=ticket.I("_v")}),tech)).AsObject();
Assert(ticket.S("firstResponseAt")!=""&&await store.GetFile(ticket.A("messages").Items().Last().O("file").S("key"))!=null,"Public response records time and attachment");
Assert((await store.All("notifications")).Count==beforeComments,"Normal comments and internal notes do not create noise");
await Denied(400,$"reportes/{id}/mensajes","POST",Obj(new{text="Pregunta",requiresAction=true,targetUserId="tech@example.edu",version=ticket.I("_v")}),tech,"Action requests cannot notify their author");
await Denied(403,$"reportes/{id}/mensajes","POST",Obj(new{text="Pregunta",requiresAction=true,targetUserId="other-admin@example.edu",version=ticket.I("_v")}),tech,"Action requests cannot notify another area");
ticket=(await Call($"reportes/{id}/mensajes","POST",Obj(new{text="¿Puedes comprobar la imagen?",requiresAction=true,targetUserId="student@example.edu",version=ticket.I("_v")}),tech)).AsObject();
var actionNotices=(await store.All("notifications")).Where(n=>n.S("ticketId")==id&&n.S("eventType")=="action").ToList();Assert(actionNotices.Count==1&&actionNotices[0].S("userId")=="student@example.edu","Explicit action notifies only its intended participant");
Assert(!await store.CreateOnce("notifications",actionNotices[0]),"Duplicate event and recipient insert is idempotent");
var beforeRead=(await store.All("notifications")).Count;await Call("notifications","PUT",Obj(new{id=actionNotices[0].S("id"),read=true}),student);await Call("notifications","PUT",Obj(new{id=actionNotices[0].S("id"),read=true}),student);Assert((await store.All("notifications")).Count==beforeRead,"Repeated read does not create any notification");
await Denied(400,$"reportes/{id}/mensajes","POST",Obj(new{text="Adjunto",file=new{name="x.pdf",type="application/pdf",data=Convert.ToBase64String("wrong"u8)},version=ticket.I("_v")}),tech,"Attachment content signature validated");
ticket=(await Call($"reportes/{id}/estado","PATCH",Obj(new{estado=3,version=ticket.I("_v")}),tech)).AsObject();
await Denied(400,$"reportes/{id}/estado","PATCH",Obj(new{estado=4,version=ticket.I("_v")}),tech,"Closing without documented solution rejected");
ticket=(await Call($"reportes/{id}/solucion","PUT",Obj(new{solucion="Se reemplazó el cable HDMI y se verificó la imagen.",cerrar=true,version=ticket.I("_v")}),tech)).AsObject();
Assert(ticket.I("estado")==4&&ticket.S("resolvedTechnicianId")=="tech@example.edu","Solution closes ticket and freezes responsible technician");
var resolvedNotices=(await store.All("notifications")).Where(n=>n.S("ticketId")==id&&n.S("eventType")=="resolved").ToList();Assert(resolvedNotices.Count==2&&resolvedNotices.All(n=>n.S("userId") is "student@example.edu" or "admin@example.edu"),"Resolution notifies requester and responsible admin, not technician or owner");
await Denied(403,$"reportes/{id}/valoracion","PUT",Obj(new{score=5,comment="",version=ticket.I("_v")}),tech,"Technician cannot rate own work");
ticket=(await Call($"reportes/{id}/valoracion","PUT",Obj(new{score=5,comment="Atención oportuna",version=ticket.I("_v")}),student)).AsObject();Assert(ticket.O("rating").I("score")==5,"Requester rating persisted against resolved technician");
var articleBody=Obj(new{title="Restablecer señal HDMI",body=ticket.S("solucion"),sourceTicketId=id,visibility="usuarios",status="Borrador",serviceId="service",keywords="HDMI"});
var article=(await Call("articles","POST",articleBody,tech)).AsObject();Assert(article.S("sourceTicketId")==id,"Knowledge draft linked to resolved ticket");
articleBody=Merge(article,Obj(new{status="Publicado",version=article.I("_v")}));await Denied(403,"articles","POST",articleBody,tech,"Technician cannot publish knowledge");
article=(await Call("articles","POST",articleBody,admin)).AsObject();Assert(article.A("versions").Count==1&&(await Call("data",principal:student)).A("articles").Count==0,"Publication hidden from requester and prior revision preserved");
await Call("votes","POST",Obj(new{articleId=article.S("id"),helpful=true,ticketId=id}),tech);var analytics=await Call("analytics",principal:admin);Assert(analytics.I("resolved")==1&&analytics.I("helpful")==1&&analytics.I("reused")==1,"Analytics use real closed tickets and knowledge votes");
var notices=(JsonArray)await Call("notifications",principal:student);Assert(notices.Count>0&&!notices.Items().Any(n=>n.B("internal")),"Requester receives visible notifications only");await Call("notifications","PUT",Obj(new{all=true}),student);Assert(((JsonArray)await Call("notifications",principal:student)).Items().All(n=>n.B("read")),"Mark notifications read");
var cfg=Obj(new{kind="areas",nombre="Biblioteca",activo=true});var area=(await Call("config","POST",cfg,owner)).AsObject();cfg=Merge(area,Obj(new{parentId=area.S("id"),version=area.I("_v")}));await Denied(400,"config","POST",cfg,owner,"Circular configuration hierarchy rejected");
await Denied(403,"analytics","GET",new(),tech,"Technician cannot query administrator analytics");
await Denied(403,"usuarios","GET",new(),student,"Requester cannot list user profiles");
var elevatedTech=(await store.Get("users","tech@example.edu"))!;elevatedTech["permissions"]=Obj(new{gestionarUsuarios=true,usuarios=true});await store.Save("users",elevatedTech);
await Denied(403,"usuarios","POST",Obj(new{correo="forbidden@example.edu",nombre="No",password}),tech,"User-management override cannot bypass technician role boundary");
elevatedTech=(await store.Get("users","tech@example.edu"))!;elevatedTech["permissions"]=new JsonObject();await store.Save("users",elevatedTech);
await Denied(403,"config","POST",Obj(new{kind="areas",nombre="No autorizada"}),tech,"Technician cannot alter configuration");
await Denied(403,"mail","GET",new(),admin,"Administrator cannot access owner mail settings");
await Denied(403,"usuarios/owner%40example.edu","PUT",Obj(new{nombre="Cambio ajeno"}),admin,"Administrator cannot alter owner account");
await Denied(400,"usuarios/owner%40example.edu","PUT",Obj(new{nombre="Propietario",activo=false}),owner,"Owner cannot deactivate their own account");
var managed=await Call("usuarios","POST",Obj(new{correo="managed@example.edu",nombre="Cuenta QA",role="Solicitante",password,areas=new[]{"area"},areaId="area",operationalAreas=new[]{"area"}}),admin);Assert(managed.B("mustChange")&&managed["passwordHash"]==null,"Admin creates requester without exposing password hash");
managed=await Call("usuarios/managed%40example.edu","PUT",Obj(new{nombre="Cuenta QA actualizada",activo=false,version=managed.I("version")}),admin);Assert(!managed.B("activo")&&managed.S("nombre")=="Cuenta QA actualizada","Admin edits and deactivates requester persistently");
var privileged=await Call("usuarios","POST",Obj(new{correo="managed-tech@example.edu",nombre="Técnico QA",role="Técnico",password}),owner);Assert(privileged.S("role")=="Técnico","Owner can create technician role");
await Denied(403,"articles","POST",Merge(article,Obj(new{status="Borrador",version=article.I("_v")})),tech,"Technician cannot edit a published article by reverting to draft");
await Denied(409,"articles","POST",Merge(article,Obj(new{version=0})),admin,"Stale published article update rejected");
Assert(BusinessCalendar.Due("2026-10-09T21:30:00Z",2,Obj(new{timezone="America/Guayaquil",days=new[]{1,2,3,4,5},start="08:00",end="17:00",holidays=Array.Empty<string>()})).StartsWith("2026-10-12T14:30"),"Business SLA skips weekend in institutional timezone");
var roleDoc=await store.Get("roles","Solicitante");await Call("permisos","PUT",Obj(new{type="role",id="Solicitante",permissions=new{crear=false},scope="propios"}),owner);await Denied(401,"data","GET",new(),student,"Permission changes invalidate existing sessions");student=await SignIn("student@example.edu");await Denied(403,"reportes","POST",TicketBody(),student,"Updated role restrictions enforced server side");
var user=await store.Get("users","student@example.edu");await Call("usuarios/student%40example.edu/reset","PUT",Obj(new{password="NuevaClaveSegura123#",version=user!.I("_v")}),admin);await Denied(401,"auth/me","GET",new(),student,"Password reset revokes sessions");var resetLogin=await Call("auth/login","POST",Obj(new{correo="student@example.edu",password="NuevaClaveSegura123#"}));student=Principal(resetLogin);Assert(resetLogin.B("mustChange"),"Reset password requires first-login change");await Denied(403,"data","GET",new(),student,"Forced password change blocks business actions");await Call("auth/password","POST",Obj(new{current="NuevaClaveSegura123#",password="OtraClaveSegura123#"}),student);await Denied(401,"auth/me","GET",new(),student,"Changing password invalidates session");
await Call("auth/logout","POST",new(),tech);await Denied(401,"auth/me","GET",new(),tech,"Logout revokes JWT session");
for(int i=0;i<5;i++)await Denied(401,"auth/login","POST",Obj(new{correo="tech@example.edu",password="incorrecta"}),new(),"Failed login counted "+(i+1));await Denied(423,"auth/login","POST",Obj(new{correo="tech@example.edu",password}),new(),"Account locks after five failures");
await Call($"reportes/{id}","DELETE",new(),admin);Assert((await Call("data",principal:admin)).A("tickets").Count==0&&(await store.Get("tickets",id))!.A("historial").Count>0,"Archive hides ticket and preserves its history");
if(transaction!=null)await transaction.RollbackAsync();
if(api!=null){await api.ProtocolChecks();checks+=4;}
Console.WriteLine($"SUCCESS: {checks} checks. Storage: {(httpMode?"HTTP + PostgreSQL local disposable database; synthetic data retained":pg?"PostgreSQL; test data rolled back":"isolated memory; PostgreSQL NOT validated")}.");
class NoNetwork:IHttpClientFactory {public HttpClient CreateClient(string name)=>throw new Exception("Network is forbidden in business checks");}
class MemoryStore:IStore {
 readonly Dictionary<string,JsonObject> records=new();readonly Dictionary<string,long> sequences=new();public Dictionary<string,Archivo> Files=new();
 public Task<List<JsonObject>> All(string kind)=>Task.FromResult(records.Where(k=>k.Key.StartsWith(kind+"/",StringComparison.Ordinal)).Select(k=>k.Value.Copy()).ToList());
 public Task<JsonObject?> Get(string kind,string id)=>Task.FromResult(records.TryGetValue(kind+"/"+id,out var value)?value.Copy():null);
 public Task<JsonObject> Save(string kind,JsonObject value){var key=kind+"/"+value.S("id");var old=records.GetValueOrDefault(key);Require(value.I("_v")==old.I("_v"),409,"Conflict");var j=value.Copy();j["_v"]=old.I("_v")+1;records[key]=j;return Task.FromResult(j.Copy());}
 public Task<long> Next(string name){sequences[name]=sequences.GetValueOrDefault(name)+1;return Task.FromResult(sequences[name]);}
 public Task PutFile(Archivo file){Files.Add(file.Key,file);return Task.CompletedTask;}
 public Task<Archivo?> GetFile(string key)=>Task.FromResult(Files.GetValueOrDefault(key));
}
