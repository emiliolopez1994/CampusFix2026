namespace CampusFix.Continuity;

public sealed class State {
    public int Version {get;set;}
    public List<Room> Rooms {get;set;}=[];
    public List<Activity> Activities {get;set;}=[];
    public List<IncidentLink> Links {get;set;}=[];
    public List<Decision> Decisions {get;set;}=[];
}
public sealed record Room(string Id,string AreaId,string Name,string Campus,int Capacity,string[] Equipment,bool Accessible,bool Archived=false,string Origin="manual");
public sealed record Activity(string Id,string RoomId,string Name,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Participants,string[] Requirements,bool NeedsAccessibility,bool Archived=false,string Origin="manual");
public sealed record IncidentLink(string Id,string RoomId,string TicketId,bool Archived=false,string Origin="manual");
public sealed record Decision(string Id,string ActivityId,string SourceRoomId,string TargetRoomId,string Actor,DateTimeOffset ApprovedAt,string[] TicketIds,string Status="approved",string Outcome="",int ActualMinutes=0,string Note="",string OutcomeActor="",DateTimeOffset? OutcomeAt=null);
public sealed record TicketSnapshot(string Id,string AreaId,string Subject,int Priority);
public sealed record Scope(string Actor,string[] Areas,TicketSnapshot[] Tickets);
public sealed record Request(Scope Scope,Command? Command=null);
public sealed class Command {
    public string Kind {get;set;}="";
    public int Version {get;set;}=-1;
    public string Id {get;set;}="";
    public string AreaId {get;set;}="";
    public string Name {get;set;}="";
    public string Campus {get;set;}="";
    public int Capacity {get;set;}
    public string[] Equipment {get;set;}=[];
    public bool Accessible {get;set;}
    public string RoomId {get;set;}="";
    public DateTimeOffset StartsAt {get;set;}
    public DateTimeOffset EndsAt {get;set;}
    public int Participants {get;set;}
    public string[] Requirements {get;set;}=[];
    public bool NeedsAccessibility {get;set;}
    public string TicketId {get;set;}="";
    public string TargetRoomId {get;set;}="";
    public string Outcome {get;set;}="";
    public int ActualMinutes {get;set;}
    public string Note {get;set;}="";
}
public sealed record Impact(Activity Activity,Room Room,TicketSnapshot[] Tickets,Room[] Alternatives,string Urgency,string Reason,double PersonHours);
public sealed class RuleError(int status,string message):Exception(message) { public int Status {get;}=status; }
