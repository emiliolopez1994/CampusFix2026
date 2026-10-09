using System.Text.Json.Nodes;
using CampusFix.Api.Models;
using CampusFix.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static CampusFix.Api.Services.J;
namespace CampusFix.Api.Data;
public interface IStore {
 Task<List<JsonObject>> All(string kind);
 Task<JsonObject?> Get(string kind,string id);
 Task<JsonObject> Save(string kind,JsonObject value);
 Task<long> Next(string name);
 Task PutFile(Archivo file);
 Task<Archivo?> GetFile(string key);
}
public class Store(CampusFixDbContext db):IStore {
 static JsonObject Read(Documento d){var j=JsonNode.Parse(d.Data)!.AsObject();j["id"]=d.Id;j["_v"]=d.Version;return j;}
 static JsonObject Read(Usuario u){var j=JsonNode.Parse(u.Profile)!.AsObject();j["id"]=u.Id;j["correo"]=u.Email;j["_v"]=u.Version;j["passwordHash"]=u.PasswordHash;j["stamp"]=u.SecurityStamp;j["failures"]=u.AccessFailedCount;j["lockUntil"]=u.LockoutEnd?.ToString("O");return j;}
 public async Task<List<JsonObject>> All(string kind)=>kind=="users"?(await db.Users.AsNoTracking().ToListAsync()).Select(Read).ToList():(await db.Documentos.AsNoTracking().Where(x=>x.Kind==kind).ToListAsync()).Select(Read).ToList();
 public async Task<JsonObject?> Get(string kind,string id){if(kind=="users"){var u=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);return u==null?null:Read(u);}var d=await db.Documentos.AsNoTracking().SingleOrDefaultAsync(x=>x.Kind==kind&&x.Id==id);return d==null?null:Read(d);}
 public async Task<JsonObject> Save(string kind,JsonObject value){
  var j=value.Copy();string id=j.S("id");Require(id.Length>0,400,"Identificador requerido.");var version=j.I("_v");j.Remove("id");j.Remove("_v");j.Remove("version");
  if(kind=="users"){
   var u=await db.Users.SingleOrDefaultAsync(x=>x.Id==id);Require(u==null?version==0:u.Version==version,409,"El usuario cambió. Actualiza antes de guardar.");
   if(u==null){u=new Usuario{Id=id};db.Users.Add(u);}
   u.Email=j.S("correo",id);u.UserName=u.Email;u.NormalizedEmail=u.Email.ToUpperInvariant();u.NormalizedUserName=u.NormalizedEmail;u.EmailConfirmed=true;u.PasswordHash=j.S("passwordHash");u.SecurityStamp=j.S("stamp");u.ConcurrencyStamp=Id();u.LockoutEnabled=true;u.AccessFailedCount=j.I("failures");u.LockoutEnd=string.IsNullOrEmpty(j.S("lockUntil"))?null:Date(j.S("lockUntil"));u.Version=version+1;
   foreach(var k in new[]{"correo","passwordHash","stamp","failures","lockUntil"})j.Remove(k);u.Profile=j.ToJsonString();
   var roleName=j.S("role");var role=await db.Roles.SingleOrDefaultAsync(r=>r.Name==roleName);if(role==null){role=new IdentityRole(roleName){Id=roleName,NormalizedName=roleName.ToUpperInvariant()};db.Roles.Add(role);}
   var memberships=await db.UserRoles.Where(r=>r.UserId==id).ToListAsync();db.UserRoles.RemoveRange(memberships.Where(r=>r.RoleId!=role.Id));if(!memberships.Any(r=>r.RoleId==role.Id))db.UserRoles.Add(new(){UserId=id,RoleId=role.Id});
   await db.SaveChangesAsync();db.ChangeTracker.Clear();return Read(u);
  }
  var d=await db.Documentos.SingleOrDefaultAsync(x=>x.Kind==kind&&x.Id==id);Require(d==null?version==0:d.Version==version,409,"El registro cambió. Actualiza antes de guardar.");if(d==null){d=new(){Kind=kind,Id=id};db.Documentos.Add(d);}d.Data=j.ToJsonString();d.Version=version+1;await db.SaveChangesAsync();db.ChangeTracker.Clear();return Read(d);
 }
 public async Task<long> Next(string name){
  // PostgreSQL performs this allocation atomically, including concurrent requests.
  await db.Database.ExecuteSqlInterpolatedAsync($"""INSERT INTO "Secuencias" ("Name","Value") VALUES ({name},0) ON CONFLICT ("Name") DO NOTHING""");
  var values=await db.Database.SqlQuery<long>($"""
   UPDATE "Secuencias" SET "Value"="Value"+1 WHERE "Name"={name} RETURNING "Value"
   """).ToListAsync();return values.Single();
 }
 public async Task PutFile(Archivo f){db.Archivos.Add(f);await db.SaveChangesAsync();db.ChangeTracker.Clear();}
 public Task<Archivo?> GetFile(string key)=>db.Archivos.AsNoTracking().SingleOrDefaultAsync(x=>x.Key==key);
}
