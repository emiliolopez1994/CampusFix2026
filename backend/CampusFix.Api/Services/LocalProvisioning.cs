using System.Text.Json.Nodes;
using System.Security.Cryptography;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;

namespace CampusFix.Api.Services;
public partial class CampusService
{
    internal const string ProvisionBatch="institutional-2026-10-10-v1";
    internal async Task<JsonObject> ProvisionInstitution(JsonArray credentials)
    {
        var people=await s.All("users");u=people.First(x=>x.S("role")=="Superadministrador"&&x.B("activo"));p=Defaults("Superadministrador");
        var syntheticIds=people.Where(x=>x.S("syntheticOrigin")==ProvisionBatch).Select(x=>x.S("id")).ToHashSet();foreach(var ticket in (await s.All("tickets")).Where(t=>t.S("syntheticOrigin")==""&&syntheticIds.Contains(t.S("reportadoPorId")))){ticket["syntheticOrigin"]=ProvisionBatch;await s.Save("tickets",ticket);}
        var owner=u.Copy();var areas=(await s.All("config")).Where(c=>c.S("kind")=="areas"&&c.B("activo")).OrderBy(c=>c.S("id")).ToList();
        Require(areas.Count>0,400,"No hay áreas institucionales activas.");var report=new JsonArray();
        string Key(string area,string part)=>"local-"+Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(area))).ToLowerInvariant()[..12]+"-"+part;
        async Task<JsonObject> Person(string id,string name,string role,string area){
            var existing=await s.Get("users",id);if(existing!=null){Require(existing.S("role")==role&&existing.B("activo"),409,"Cuenta del lote desactivada o con rol cambiado: "+id);return existing;}
            var password="Aa9!"+Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
            var person=await s.Save("users",Obj(new{id,correo=id,nombre=name,role,activo=true,areaId=area,areas=new[]{area},operationalAreas=role=="Técnico"?new[]{area}:Array.Empty<string>(),passwordHash=Hash(password),stamp=Id(),mustChange=false,created=Now(),syntheticOrigin=ProvisionBatch}));
            credentials.Add(Obj(new{id,nombre=name,role,areaId=area,password,policy="Acceso local inicial; cambio voluntario. Contraseña aleatoria exclusiva."}));return person;
        }
        async Task<JsonObject> Config(string stable,JsonObject body){
            var saved=await s.Get("provisioning",stable);if(saved!=null){var found=await s.Get("config",saved.S("resourceId"));Require(found!=null,409,"Recurso del lote ausente; requiere revisión.");return found!;}
            var result=(JsonObject)await ConfigApi(body);result["syntheticOrigin"]=ProvisionBatch;result=await s.Save("config",result);
            await s.Save("provisioning",Obj(new{id=stable,batch=ProvisionBatch,resourceId=result.S("id")}));return result;
        }
        var firstNames=new[]{"Lucía","Mateo","Camila","Diego","Valeria","Andrés","Elena","Nicolás"};
        var lastNames=new[]{"Varela","Salcedo","Montes","Cabrera","Molina","Serrano","Alvarado","Cevallos"};
        for(int a=0;a<areas.Count;a++){
            u=owner.Copy();p=Defaults("Superadministrador");var area=areas[a];var aid=area.S("id");var name=area.S("nombre");var config=await s.All("config");people=await s.All("users");
            var managers=people.Where(x=>x.B("activo")&&x.S("role")=="Administrador"&&area.A("managers").Has(x.S("id"))).ToList();
            if(managers.Count==0){
                // Reuse explicitly configured team administrators only when that team has this exact area.
                var ids=config.Where(c=>c.S("kind")=="equipos"&&c.B("activo")&&c.S("responsibleAreaId")==aid).SelectMany(c=>c.A("managers").Select(m=>m!.ToString())).ToHashSet();
                var existing=people.FirstOrDefault(x=>ids.Contains(x.S("id"))&&x.S("role")=="Administrador"&&x.B("activo"));
                managers.Add(existing??await Person(Key(aid,"admin")+"@campusfix.example","Administración de "+name,"Administrador",aid));
                foreach(var manager in managers)if(!area.A("managers").Has(manager.S("id"))){if(area["managers"] is not JsonArray)area["managers"]=new JsonArray();area.A("managers").Add(manager.S("id"));}
                area=await s.Save("config",area);
            }
            config=await s.All("config");people=await s.All("users");
            var team=config.FirstOrDefault(c=>c.S("kind")=="equipos"&&c.B("activo")&&ResourceAreas(c,config).Contains(aid));
            var technician=people.FirstOrDefault(x=>EligibleForArea(x,aid,config));
            technician??=await Person(Key(aid,"responsable")+"@campusfix.example",firstNames[a%8]+" "+lastNames[(a+3)%8],"Técnico",aid);
            if(!technician.A("operationalAreas").Has(aid)){if(technician["operationalAreas"] is not JsonArray)technician["operationalAreas"]=new JsonArray();technician.A("operationalAreas").Add(aid);technician=await s.Save("users",technician);}
            if(team==null)team=await Config(Key(aid,"team"),Obj(new{kind="equipos",nombre="Atención de "+name,descripcion="Equipo encargado de atender y dar seguimiento a solicitudes de "+name+".",activo=true,responsibleAreaId=aid,members=new[]{technician.S("id")},managers=managers.Select(m=>m.S("id")).ToArray()}));
            else if(!team.A("members").Has(technician.S("id"))){if(team["members"] is not JsonArray)team["members"]=new JsonArray();team.A("members").Add(technician.S("id"));team=await s.Save("config",team);}
            var services=config.Where(c=>c.S("kind")=="servicios"&&c.B("activo")&&c.S("responsibleAreaId")==aid).ToList();
            if(services.Count==0)services.Add(await Config(Key(aid,"service"),Obj(new{kind="servicios",nombre="Atención de "+name,descripcion="Consultas y solicitudes sobre los servicios de "+name+".",activo=true,responsibleAreaId=aid,teamId=team.S("id")})));
            if(name.Contains("Bienestar",StringComparison.OrdinalIgnoreCase)&&!services.Any(c=>c.S("nombre").Equals("Apoyo estudiantil",StringComparison.OrdinalIgnoreCase)))services.Add(await Config(Key(aid,"apoyo"),Obj(new{kind="servicios",nombre="Apoyo estudiantil",descripcion="Consultas sobre servicios y recursos de apoyo al estudiante.",activo=true,responsibleAreaId=aid,teamId=team.S("id")})));
            var calendar=config.FirstOrDefault(c=>c.S("kind")=="calendarios"&&c.B("activo")&&ResourceAreas(c,config).Contains(aid));
            calendar??=await Config(Key(aid,"calendar"),Obj(new{kind="calendarios",nombre="Horario de atención de "+name,descripcion="Horario operativo local de atención universitaria.",activo=true,responsibleAreaId=aid,timezone="America/Guayaquil",days=new[]{1,2,3,4,5},start="08:00",end="17:00",holidays=Array.Empty<string>()}));
            var categories=new List<JsonObject>();
            foreach(var service in services){
                if(service.S("teamId")==""){service["teamId"]=team.S("id");await s.Save("config",service);}
                var sc=config.Where(c=>c.S("kind")=="categorias"&&c.B("activo")&&c.S("serviceId")==service.S("id")).ToList();
                if(sc.Count==0)sc.Add(await Config(Key(service.S("id"),"category"),Obj(new{kind="categorias",nombre=service.S("nombre")=="Apoyo estudiantil"?"Información de beneficios":"Consultas y seguimiento",descripcion="Atención y seguimiento de solicitudes de "+service.S("nombre")+".",activo=true,serviceId=service.S("id"),teamId=team.S("id")})));
                categories.AddRange(sc);
            }
            for(var priority=1;priority<=4;priority++)if(!config.Any(c=>c.S("kind")=="metas"&&c.B("activo")&&c.I("prioridad")==priority&&ResourceAreas(c,config).Contains(aid)))await Config(Key(aid,"meta"+priority),Obj(new{kind="metas",nombre="Atención "+new[]{"","baja","media","alta","crítica"}[priority]+" · "+name,descripcion="Objetivo operativo local de primera respuesta y resolución.",activo=true,responsibleAreaId=aid,calendarId=calendar.S("id"),prioridad=priority,responseHours=priority>=3?2:8,resolutionHours=priority>=3?8:24}));
            var ticketIds=new List<string>();var requesterIds=new List<string>();
            for(var i=0;i<5;i++){
                var belonging=areas[(a+i+1)%areas.Count].S("id");var person=await Person(Key(aid,"solicitante"+i)+"@campusfix.example",firstNames[(a+i)%8]+" "+lastNames[(a*3+i)%8]+" "+lastNames[(a+i+2)%8],"Solicitante",belonging);requesterIds.Add(person.S("id"));
                var marker=Key(aid,"ticket"+i);var previous=await s.Get("provisioning",marker);
                if(previous!=null){
                    var saved=await s.Get("tickets",previous.S("ticketId"));Require(saved!=null,409,"Solicitud del lote ausente; no se recrea automáticamente.");
                    var title=ServiceTitle(name,saved!.S("serviceNombre"),i);
                    // Correct only untouched synthetic initial content; never rewrite authentic history.
                    if(saved.S("syntheticOrigin")==ProvisionBatch&&saved.I("estado")==1&&saved.S("tecnicoId")==""&&saved.A("messages").Count==0&&saved.S("problema")!=title){
                        saved["problema"]=title;saved["descripcion"]="Solicito apoyo de "+name+" para "+title.ToLowerInvariant()+". Agradezco información sobre el procedimiento y los próximos pasos.";await s.Save("tickets",saved);
                        foreach(var notice in (await s.All("notifications")).Where(n=>n.S("ticketId")==saved.S("id")&&n.S("eventType")=="created")){notice["title"]=$"Nuevo reporte CF-{saved.S("id").PadLeft(4,'0')}: {title}";await s.Save("notifications",notice);}
                    }
                    ticketIds.Add(previous.S("ticketId"));continue;
                }
                var service=services[i%services.Count];var category=categories.First(c=>c.S("serviceId")==service.S("id"));var custom=new JsonObject();
                foreach(var field in service.A("fields").Items().Where(f=>!f.B("internal")))custom[field.S("id")]=field.S("type")=="select"?field.A("options").FirstOrDefault()?.ToString()??"":field.S("type")=="number"?"1":"Información de "+service.S("nombre");
                var subject=ServiceTitle(name,service.S("nombre"),i);u=person;p=await Effective(s,u);var ticket=(JsonObject)await CreateTicket(Obj(new{problema=subject,ubicacion="Atención en línea",descripcion="Solicito apoyo de "+name+" para "+subject.ToLowerInvariant()+". Agradezco información sobre el procedimiento, los horarios disponibles y los próximos pasos.",prioridad=2,areaId=belonging,attentionAreaId=aid,serviceId=service.S("id"),categoryId=category.S("id"),custom}));
                ticket["syntheticOrigin"]=ProvisionBatch;ticket["provisionKey"]=marker;await s.Save("tickets",ticket);ticketIds.Add(ticket.S("id"));
                await s.Save("provisioning",Obj(new{id=marker,batch=ProvisionBatch,areaId=aid,ticketId=ticket.S("id"),requesterId=person.S("id")}));u=owner.Copy();p=Defaults("Superadministrador");
            }
            report.Add(Obj(new{areaId=aid,area=name,administratorIds=managers.Select(m=>m.S("id")).ToArray(),teamId=team.S("id"),responsibleId=technician.S("id"),serviceIds=services.Select(c=>c.S("id")).ToArray(),categoryIds=categories.Select(c=>c.S("id")).ToArray(),calendarId=calendar.S("id"),requesterIds,ticketIds}));
        }
        return Obj(new{batch=ProvisionBatch,areas=report});
    }
    static string ServiceTitle(string area,string service,int i)
    {
        if(service.Contains("Plataformas",StringComparison.OrdinalIgnoreCase))return new[]{"Acceso al aula virtual desde equipo institucional","Consulta sobre ingreso a plataforma académica","Asistencia con materiales del aula virtual","Seguimiento de acceso a curso en línea","Orientación sobre uso de plataforma académica"}[i];
        if(service.Contains("accesos",StringComparison.OrdinalIgnoreCase))return new[]{"Orientación para recuperar acceso institucional","Recuperación de cuenta institucional bloqueada","Consulta sobre cambio de contraseña institucional","Asistencia con validación de acceso a la cuenta","Seguimiento de recuperación de credenciales"}[i];
        if(service.Contains("Soporte tecnol",StringComparison.OrdinalIgnoreCase))return new[]{"Computador de consulta no inicia correctamente","Teclado de equipo institucional sin respuesta","Pantalla de estación de trabajo sin imagen","Revisión de periféricos en sala de estudio","Asistencia con conexión de monitor externo"}[i];
        if(service.Contains("Orientación estudiantil",StringComparison.OrdinalIgnoreCase))return new[]{"Solicitud de orientación sobre planificación académica","Consulta de horarios de orientación académica","Orientación sobre organización de carga académica","Acompañamiento para adaptación a la vida universitaria","Seguimiento de orientación académica"}[i];
        if(service.Contains("Actividades de bienestar",StringComparison.OrdinalIgnoreCase))return new[]{"Información sobre actividades de integración","Consulta de horarios de acompañamiento grupal","Inscripción a taller de habilidades de estudio","Información sobre programas de bienestar","Seguimiento de inscripción a actividad estudiantil"}[i];
        if(service.Contains("Apoyo estudiantil",StringComparison.OrdinalIgnoreCase))return new[]{"Consulta sobre recursos de apoyo estudiantil","Información de beneficios para estudiantes","Orientación sobre servicios de apoyo universitario","Consulta de horarios de atención de beneficios","Seguimiento a solicitud de apoyo estudiantil"}[i];
        return Topics(area)[i];
    }
    static string[] Topics(string area)
    {
        if(area.Contains("Bienestar",StringComparison.OrdinalIgnoreCase))return ["Solicitud de orientación sobre planificación académica","Consulta de horarios de acompañamiento estudiantil","Inscripción a taller de habilidades de estudio","Información sobre actividades de integración universitaria","Seguimiento a solicitud de apoyo estudiantil"];
        if(area.Contains("Tecnolog",StringComparison.OrdinalIgnoreCase))return ["Conexión inalámbrica intermitente en sala de estudio","Impresora de biblioteca no recibe documentos","Acceso al aula virtual desde equipo institucional","Configuración de proyector para seminario","Asistencia para recuperar acceso institucional"];
        if(area.Contains("Mantenimiento",StringComparison.OrdinalIgnoreCase))return ["Luminaria apagada en pasillo del bloque académico","Revisión de grifo en baño de biblioteca","Ajuste de cerradura en sala de reuniones","Mantenimiento de ventilación en aula","Reparación de mesa en zona de lectura"];
        if(area.Contains("Biblioteca",StringComparison.OrdinalIgnoreCase))return ["Consulta de disponibilidad de bibliografía del curso","Orientación para renovar préstamo de libros","Reserva de sala de estudio grupal","Acceso al catálogo digital universitario","Consulta sobre horarios de devolución"];
        if(area.Contains("Admisiones",StringComparison.OrdinalIgnoreCase))return ["Información de requisitos de ingreso universitario","Consulta sobre fechas de inscripción","Orientación para entrega de documentos de admisión","Disponibilidad de jornadas informativas","Seguimiento de consulta de oferta académica"];
        if(area.Contains("Registro",StringComparison.OrdinalIgnoreCase))return ["Orientación sobre consulta de matrícula","Información para solicitar constancia de estudios","Consulta de calendario de registro de asignaturas","Procedimiento de actualización de datos académicos","Seguimiento de solicitud de certificado académico"];
        if(area.Contains("Secretaría",StringComparison.OrdinalIgnoreCase))return ["Consulta de horarios de atención de secretaría","Orientación sobre recepción de solicitudes académicas","Información sobre calendario institucional","Procedimiento para presentar una petición académica","Seguimiento de consulta de trámites estudiantiles"];
        return ["Consulta de horarios de atención de "+area,"Orientación sobre servicios de "+area,"Solicitud de reunión informativa con "+area,"Información sobre procedimientos de "+area,"Seguimiento de consulta presentada a "+area];
    }
}
