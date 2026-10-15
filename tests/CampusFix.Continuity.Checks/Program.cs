using CampusFix.Continuity;
using Npgsql;
using System.Text.Json;

var now=DateTimeOffset.UtcNow;var state=new State();var count=0;
void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
void Denied(Action action,int status,string message){try{action();throw new Exception("No se rechazó: "+message);}catch(RuleError e){Check(e.Status==status,message);}}
var scope=new Scope("admin-a",["a"],[new("t1","a","Proyector",2),new("t2","a","Red",3),new("t3","a","Equipo",2)]);
state.Rooms.AddRange([
 new("r1","a","Origen","Central",40,["proyector"],true),new("r2","a","Alternativa","Central",40,["proyector"],true),
 new("r3","a","Otra incidencia","Central",40,["proyector"],true),new("r4","a","Pequeña","Central",10,["proyector"],true),
 new("r5","a","Sin equipo","Central",40,[],true),new("r6","a","Ocupada","Central",40,["proyector"],true),
 new("r7","b","Otra área","Central",40,["proyector"],true),new("r8","a","Otro campus","Norte",40,["proyector"],true),new("r9","a","Sin acceso","Central",40,["proyector"],false)]);
state.Activities.AddRange([new("a1","r1","Clase uno",now.AddHours(1),now.AddHours(2),30,["proyector"],true),new("a2","r3","Clase dos",now.AddHours(1),now.AddHours(2),30,["proyector"],true),new("a3","r6","Reserva",now.AddHours(1),now.AddHours(2),30,[],false)]);
state.Links.AddRange([new("l1","r1","t1"),new("l2","r1","t2"),new("l3","r3","t3")]);
var impact=Engine.Analyze(state,scope,now);
Check(impact.Count==2&&impact.First().Tickets.Length==2,"agrupa incidencias por actividad sin duplicar asistentes");
Check(impact.First().Alternatives.Select(r=>r.Id).SequenceEqual(["r2"]),"descarta falta de capacidad/equipamiento/accesibilidad, ocupación, otra área y campus");
Check(Engine.Analyze(state,scope with{Tickets=[]},now).Count==0,"ticket cerrado o eliminado deja de generar impacto");
Check(Engine.Analyze(state,scope with{Areas=["b"]},now).Count==0,"impacto respeta alcance de área");
Denied(()=>Engine.Apply(state,scope,new(){Kind="room.archive",Id="r7",Version=state.Version},now),403,"administrador no modifica otras áreas");
Denied(()=>Engine.Apply(state,scope,new(){Kind="activity.save",RoomId="r2",Name="Cruce",StartsAt=now.AddHours(1),EndsAt=now.AddHours(2),Participants=50,Version=state.Version},now),400,"rechaza actividad con exceso de capacidad");
var version=state.Version;
Engine.Apply(state,scope,new(){Kind="decision.approve",Id="a1",TargetRoomId="r2",Version=version},now);
Check(Engine.EffectiveRoom(state,state.Activities[0])=="r2","aprobación reserva la alternativa sin cambiar el ticket");
Denied(()=>Engine.Apply(state,scope,new(){Kind="decision.approve",Id="a2",TargetRoomId="r2",Version=version},now),409,"rechaza versión obsoleta");
Denied(()=>Engine.Apply(state,scope,new(){Kind="decision.approve",Id="a2",TargetRoomId="r2",Version=state.Version},now),409,"evita doble reserva de la alternativa");
Check(Engine.Analyze(state,scope,now).Count==1,"actividad con alternativa deja de estar afectada");
var decision=state.Decisions.Single();
Denied(()=>Engine.Apply(state,scope,new(){Kind="decision.outcome",Id=decision.Id,Outcome="continuada",Note="Confirmado",Version=state.Version},now),409,"no inventa resultados antes de la actividad");
Engine.Apply(state,scope,new(){Kind="decision.outcome",Id=decision.Id,Outcome="continuada",ActualMinutes=5,Note="Observación confirmada",Version=state.Version},now.AddHours(2));
Check(state.Decisions[0].Outcome=="continuada"&&state.Decisions[0].OutcomeActor==scope.Actor,"conserva resultado y autor de la observación");
Engine.Apply(state,scope,new(){Kind="room.save",AreaId="a",Name="Sala práctica",Campus="Central",Capacity=35,Equipment=["Proyector"],Accessible=true,Version=state.Version},now);
var addedRoom=state.Rooms.Last();
Engine.Apply(state,scope,new(){Kind="activity.save",RoomId=addedRoom.Id,Name="Taller práctico",StartsAt=now.AddDays(1),EndsAt=now.AddDays(1).AddHours(1),Participants=20,Requirements=["proyector"],Version=state.Version},now);
var addedActivity=state.Activities.Last();
Engine.Apply(state,scope,new(){Kind="link.save",RoomId=addedRoom.Id,TicketId="t1",Version=state.Version},now);
Check(Engine.Analyze(state,scope,now).Any(i=>i.Activity.Id==addedActivity.Id),"registro de aula, actividad y vínculo alimenta el análisis");
Denied(()=>Engine.Apply(state,scope,new(){Kind="room.archive",Id=addedRoom.Id,Version=state.Version},now),409,"no archiva un aula con dependencias");
Engine.Apply(state,scope,new(){Kind="activity.archive",Id=addedActivity.Id,Version=state.Version},now);
Engine.Apply(state,scope,new(){Kind="link.archive",Id=state.Links.Last().Id,Version=state.Version},now);
Engine.Apply(state,scope,new(){Kind="room.archive",Id=addedRoom.Id,Version=state.Version},now);
Check(state.Rooms.Last().Archived&&state.Activities.Last().Archived&&state.Links.Last().Archived,"archivo lógico conserva aula, actividad y vínculo");
var examples=new State();Seed.Apply(examples,[new("area","Área ficticia")],now);var first=JsonSerializer.Serialize(examples,Repository.Json);Seed.Apply(examples,[new("area","Área ficticia")],now.AddDays(1));Check(JsonSerializer.Serialize(examples,Repository.Json)==first,"aprovisionamiento idempotente conserva horarios e IDs");
if(Environment.GetEnvironmentVariable("CONTINUITY_TEST_CONNECTION") is {Length:>0} connection){
 var cs=new NpgsqlConnectionStringBuilder(connection);if(cs.Host is not "localhost" and not "127.0.0.1"||!(cs.Database??"").StartsWith("campusfix_continuity_qa"))throw new Exception("Solo se admite PostgreSQL aislado de QA.");
 await using(var source=NpgsqlDataSource.Create(connection)){
  var repo=new Repository(source);await repo.Initialize();var empty=await repo.Read();Check(empty.Version==0&&empty.Rooms.Count==0,"base QA vacía y aislada");
  await repo.Change(s=>{s.Version=state.Version;s.Rooms=state.Rooms;s.Activities=state.Activities;s.Links=state.Links;s.Decisions=state.Decisions;});
 }
 await using(var source=NpgsqlDataSource.Create(connection)){
  var repo=new Repository(source);var saved=await repo.Read();Check(saved.Decisions.Single().Outcome=="continuada"&&saved.Rooms.Count==state.Rooms.Count,"PostgreSQL conserva aulas, decisión y resultado después de reconectar");
  var before=JsonSerializer.Serialize(saved,Repository.Json);
  try{await repo.Change(s=>{s.Rooms.Clear();throw new RuleError(409,"rollback");});}catch(RuleError){}
  Check(JsonSerializer.Serialize(await repo.Read(),Repository.Json)==before,"transacción fallida no pierde información");
 }
}
Console.WriteLine($"{count} comprobaciones de continuidad correctas.");
