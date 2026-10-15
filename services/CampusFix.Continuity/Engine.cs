namespace CampusFix.Continuity;

public static class Engine {
    public static void Need(bool ok,string message,int status=400){if(!ok)throw new RuleError(status,message);}
    static string Text(string? text,int max=160){Need(!string.IsNullOrWhiteSpace(text)&&text.Trim().Length<=max,"Completa los campos de texto con una longitud válida.");return text!.Trim();}
    static string[] Tags(string[]? items){Need(items!=null&&items.Length<=20&&items.All(x=>!string.IsNullOrWhiteSpace(x)&&x.Length<=60),"Equipamiento no válido (máximo 20 elementos).");return items!.Select(x=>x.Trim().ToLowerInvariant()).Distinct().ToArray();}
    public static Room Allowed(State s,Scope scope,string id){var r=s.Rooms.FirstOrDefault(r=>r.Id==id&&!r.Archived);Need(r!=null&&scope.Areas.Contains(r.AreaId),"Aula no disponible en tus áreas.",403);return r!;}
    static Decision? Approval(State s,Activity a)=>s.Decisions.LastOrDefault(d=>d.ActivityId==a.Id&&d.Status=="approved");
    public static string EffectiveRoom(State s,Activity a)=>Approval(s,a)?.TargetRoomId??a.RoomId;
    static bool Overlap(Activity a,Activity b)=>a.StartsAt<b.EndsAt&&b.StartsAt<a.EndsAt;
    static bool Occupied(State s,string roomId,Activity a)=>s.Activities.Any(b=>!b.Archived&&b.Id!=a.Id&&EffectiveRoom(s,b)==roomId&&Overlap(a,b));
    static TicketSnapshot[] Incidents(State s,Scope scope,string roomId)=>scope.Tickets.Where(t=>s.Links.Any(l=>!l.Archived&&l.RoomId==roomId&&l.TicketId==t.Id)).DistinctBy(t=>t.Id).ToArray();
    public static List<Impact> Analyze(State s,Scope scope,DateTimeOffset now){
        var result=new List<Impact>();
        foreach(var a in s.Activities.Where(a=>!a.Archived&&a.EndsAt>now&&a.StartsAt<now.AddDays(7))){
            var room=s.Rooms.FirstOrDefault(r=>!r.Archived&&r.Id==EffectiveRoom(s,a)&&scope.Areas.Contains(r.AreaId));
            if(room==null)continue;
            var tickets=Incidents(s,scope,room.Id);if(tickets.Length==0)continue;
            var alternatives=s.Rooms.Where(r=>!r.Archived&&r.Id!=room.Id&&scope.Areas.Contains(r.AreaId)&&r.AreaId==room.AreaId&&r.Campus.Equals(room.Campus,StringComparison.OrdinalIgnoreCase)&&r.Capacity>=a.Participants&&(!a.NeedsAccessibility||r.Accessible)&&a.Requirements.All(q=>r.Equipment.Contains(q))&&!Occupied(s,r.Id,a)&&Incidents(s,scope,r.Id).Length==0).OrderBy(r=>r.Capacity).ThenBy(r=>r.Name).Take(5).ToArray();
            var hours=(a.StartsAt-now).TotalHours;
            var urgency=hours<=2?"Inmediata":hours<=24?"Hoy":"Planificar";
            var reason=hours<=0?"Actividad en curso con una incidencia abierta vinculada.":$"La actividad comienza en {Math.Ceiling(hours)} horas y tiene {a.Participants} participantes previstos.";
            result.Add(new(a,room,tickets,alternatives,urgency,reason,Math.Round((a.EndsAt-(a.StartsAt<now?now:a.StartsAt)).TotalHours*a.Participants,1)));
        }
        return result.OrderBy(x=>x.Activity.StartsAt).ToList();
    }
    public static object View(State s,Scope scope,DateTimeOffset now){
        var rooms=s.Rooms.Where(r=>!r.Archived&&scope.Areas.Contains(r.AreaId)).ToArray();var roomIds=rooms.Select(r=>r.Id).ToHashSet();
        var activities=s.Activities.Where(a=>!a.Archived&&roomIds.Contains(a.RoomId)).ToArray();var activityIds=activities.Select(a=>a.Id).ToHashSet();
        var impact=Analyze(s,scope,now);
        return new {s.Version,generatedAt=now,rooms,activities=activities.Select(a=>new{a.Id,a.RoomId,a.Name,a.StartsAt,a.EndsAt,a.Participants,a.Requirements,a.NeedsAccessibility,a.Origin,effectiveRoomId=EffectiveRoom(s,a)}),links=s.Links.Where(l=>!l.Archived&&roomIds.Contains(l.RoomId)).Select(l=>new{l.Id,l.RoomId,l.TicketId,l.Origin,active=scope.Tickets.Any(t=>t.Id==l.TicketId)}),decisions=s.Decisions.Where(d=>activityIds.Contains(d.ActivityId)),impact,summary=new{activities=impact.Count,participantAttendances=impact.Sum(i=>i.Activity.Participants),personHours=impact.Sum(i=>i.PersonHours),confirmedContinued=s.Decisions.Count(d=>activityIds.Contains(d.ActivityId)&&d.Status=="approved"&&d.Outcome=="continuada")}};
    }
    public static void Apply(State s,Scope scope,Command c,DateTimeOffset now){
        Need(c.Version==s.Version,"Los datos cambiaron. Actualiza antes de guardar.",409);
        Need(s.Rooms.Count+s.Activities.Count+s.Links.Count+s.Decisions.Count<20000,"Se alcanzó el límite local de registros.");
        var id=string.IsNullOrEmpty(c.Id)?Guid.NewGuid().ToString():c.Id;
        switch(c.Kind){
            case "room.save": {
                var old=s.Rooms.FirstOrDefault(r=>r.Id==id);if(c.Id!=""){Allowed(s,scope,id);Need(old!.AreaId==c.AreaId,"El área de un aula existente se conserva.");}
                Need(scope.Areas.Contains(c.AreaId),"Área no autorizada.",403);
                var name=Text(c.Name);var campus=Text(c.Campus,100);var tags=Tags(c.Equipment);
                Need(c.Capacity is >0 and <=2000,"Capacidad válida: 1 a 2000.");
                Need(!s.Rooms.Any(r=>!r.Archived&&r.Id!=id&&r.AreaId==c.AreaId&&r.Campus.Equals(campus,StringComparison.OrdinalIgnoreCase)&&r.Name.Equals(name,StringComparison.OrdinalIgnoreCase)),"Ya existe esa aula en el campus y área.",409);
                Need(!s.Activities.Any(a=>!a.Archived&&a.EndsAt>now&&EffectiveRoom(s,a)==id&&(a.Participants>c.Capacity||a.NeedsAccessibility&&!c.Accessible||a.Requirements.Any(q=>!tags.Contains(q)))),"El cambio dejaría actividades sin capacidad o equipamiento.",409);
                var room=new Room(id,c.AreaId,name,campus,c.Capacity,tags,c.Accessible,Origin:old?.Origin??"manual");if(old!=null)s.Rooms.Remove(old);s.Rooms.Add(room);break;
            }
            case "room.archive": {
                var room=Allowed(s,scope,id);Need(!s.Activities.Any(a=>!a.Archived&&(a.RoomId==id||EffectiveRoom(s,a)==id))&&!s.Links.Any(l=>!l.Archived&&l.RoomId==id),"El aula tiene actividades o incidencias vinculadas. Conserva sus relaciones o archívalas primero.",409);s.Rooms[s.Rooms.IndexOf(room)]=room with{Archived=true};break;
            }
            case "activity.save": {
                var room=Allowed(s,scope,c.RoomId);var old=s.Activities.FirstOrDefault(a=>a.Id==id);
                if(c.Id!=""){Need(old!=null&&!old.Archived,"Actividad no encontrada.",404);Allowed(s,scope,old!.RoomId);Need(Approval(s,old)==null,"La actividad tiene una alternativa aprobada. Cancela esa decisión antes de editarla.",409);}
                Need(c.StartsAt>now.AddMinutes(-5)&&c.StartsAt<now.AddYears(2)&&c.EndsAt>c.StartsAt&&(c.EndsAt-c.StartsAt).TotalHours<=12,"Horario inválido: inicio futuro, duración máxima de 12 horas.");
                var tags=Tags(c.Requirements);Need(c.Participants>0&&c.Participants<=room.Capacity&&(!c.NeedsAccessibility||room.Accessible)&&tags.All(room.Equipment.Contains),"El aula no tiene capacidad, accesibilidad o equipamiento suficiente.");
                var activity=new Activity(id,room.Id,Text(c.Name),c.StartsAt,c.EndsAt,c.Participants,tags,c.NeedsAccessibility,Origin:old?.Origin??"manual");
                Need(!Occupied(s,room.Id,activity),"El aula está ocupada en ese horario.",409);if(old!=null)s.Activities.Remove(old);s.Activities.Add(activity);break;
            }
            case "activity.archive": {
                var a=s.Activities.FirstOrDefault(a=>a.Id==id&&!a.Archived);Need(a!=null,"Actividad no encontrada.",404);Allowed(s,scope,a!.RoomId);Need(Approval(s,a)==null,"Cancela o conserva primero la decisión de continuidad vinculada.",409);s.Activities[s.Activities.IndexOf(a)]=a with{Archived=true};break;
            }
            case "link.save": {
                var room=Allowed(s,scope,c.RoomId);Need(scope.Tickets.Any(t=>t.Id==c.TicketId&&t.AreaId==room.AreaId),"Selecciona un ticket abierto del área responsable del aula.",403);
                Need(!s.Links.Any(l=>!l.Archived&&l.RoomId==room.Id&&l.TicketId==c.TicketId),"La incidencia ya está vinculada.",409);
                s.Links.Add(new(Guid.NewGuid().ToString(),room.Id,c.TicketId));break;
            }
            case "link.archive": {
                var l=s.Links.FirstOrDefault(l=>l.Id==id&&!l.Archived);Need(l!=null,"Vínculo no encontrado.",404);Allowed(s,scope,l!.RoomId);s.Links[s.Links.IndexOf(l)]=l with{Archived=true};break;
            }
            case "decision.approve": {
                var item=Analyze(s,scope,now).FirstOrDefault(i=>i.Activity.Id==c.Id);Need(item!=null,"La actividad ya no requiere una alternativa. Actualiza.",409);
                Need(Approval(s,item!.Activity)==null,"La actividad ya tiene una alternativa aprobada.",409);
                Need(item.Alternatives.Any(r=>r.Id==c.TargetRoomId),"El aula propuesta ya no está disponible o no cumple los requisitos.",409);
                s.Decisions.Add(new(Guid.NewGuid().ToString(),item.Activity.Id,item.Room.Id,c.TargetRoomId,scope.Actor,now,item.Tickets.Select(t=>t.Id).ToArray()));break;
            }
            case "decision.cancel": case "decision.outcome": {
                var d=s.Decisions.FirstOrDefault(d=>d.Id==id&&d.Status=="approved");Need(d!=null,"Decisión no disponible.",404);Allowed(s,scope,d!.SourceRoomId);Allowed(s,scope,d.TargetRoomId);
                var a=s.Activities.First(a=>a.Id==d.ActivityId);
                if(c.Kind=="decision.cancel"){
                    Need(d.Outcome==""&&a.StartsAt>now,"Solo se cancelan decisiones sin resultado antes de comenzar.",409);Need(!Occupied(s,d.SourceRoomId,a),"El aula original tiene otra reserva en ese horario.",409);
                    s.Decisions[s.Decisions.IndexOf(d)]=d with{Status="cancelled",Note=Text(c.Note,1000)};
                }else{
                    Need(now>=a.StartsAt,"Registra el resultado cuando la actividad haya comenzado.",409);Need(d.Outcome=="","El resultado ya fue registrado.",409);
                    Need(new[]{"continuada","interrumpida","cancelada"}.Contains(c.Outcome),"Resultado no válido.");Need(c.ActualMinutes>=0&&c.ActualMinutes<=Math.Ceiling((a.EndsAt-a.StartsAt).TotalMinutes),"Minutos de interrupción fuera de la duración de la actividad.");
                    s.Decisions[s.Decisions.IndexOf(d)]=d with{Outcome=c.Outcome,ActualMinutes=c.ActualMinutes,Note=Text(c.Note,1000),OutcomeActor=scope.Actor,OutcomeAt=now};
                }break;
            }
            default:throw new RuleError(404,"Operación no disponible.");
        }
        s.Version++;
    }
}
