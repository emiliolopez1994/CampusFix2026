namespace CampusFix.Continuity;
public sealed record SeedArea(string Id,string Name,string? TicketId=null);
public static class Seed {
    public static void Apply(State s,SeedArea[] areas,DateTimeOffset now){
        // Stable IDs and provenance: reruns do not reset schedules, decisions or archived records.
        var changed=false;var start=now.AddHours(1);
        foreach(var area in areas){
            var prefix="continuity-example-"+area.Id;
            var equipment=new[]{"proyector","wifi","pizarra"};
            for(var i=1;i<=3;i++){
                var id=prefix+"-room-"+i;if(s.Rooms.Any(r=>r.Id==id))continue;
                s.Rooms.Add(new(id,area.Id,$"Sala {i:00} · {area.Name}","Campus Central",i==3?20:45,equipment,true,Origin:"synthetic-continuity-v1"));changed=true;
            }
            for(var i=1;i<=2;i++){
                var id=prefix+"-activity-"+i;if(s.Activities.Any(a=>a.Id==id))continue;
                var begins=start.AddHours((i-1)*3);
                s.Activities.Add(new(id,prefix+"-room-1",i==1?$"Taller de orientación · {area.Name}":$"Seminario de recursos institucionales · {area.Name}",begins,begins.AddHours(1.5),i==1?30:25,["proyector","wifi"],true,Origin:"synthetic-continuity-v1"));changed=true;
            }
            if(!string.IsNullOrEmpty(area.TicketId)&&!s.Links.Any(l=>l.Id==prefix+"-link")){
                s.Links.Add(new(prefix+"-link",prefix+"-room-1",area.TicketId,Origin:"synthetic-continuity-v1"));changed=true;
            }
        }
        if(changed)s.Version++;
    }
}
