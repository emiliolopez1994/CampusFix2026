using System.Text.Json.Nodes;
using CampusFix.Api.Models;
using CampusFix.Api.Services;
using Microsoft.EntityFrameworkCore;
using static CampusFix.Api.Services.J;
namespace CampusFix.Api.Data;
public static class Importer {
 public static async Task Command(string[] args,IServiceProvider services){var db=services.GetRequiredService<CampusFixDbContext>();
  if(args.Contains("--schema")){Console.WriteLine(db.Database.GenerateCreateScript());return;}
  var index=Array.IndexOf(args,"--import");if(index<0)index=Array.IndexOf(args,"--check-data");Require(index>=0&&args.Length>index+1,400,"Indica la ruta del archivo de datos.");var root=JsonNode.Parse(await File.ReadAllTextAsync(args[index+1]))!.AsObject();var docs=root.A("documents").Items().ToList();
  Require(docs.All(d=>d.S("kind")!="sessions"),400,"El respaldo no debe incluir sesiones.");
  if(args.Contains("--check-data")){foreach(var d in docs)JsonNode.Parse(d.S("data"));Console.WriteLine("JSON válido. "+string.Join(", ",docs.GroupBy(d=>d.S("kind")).Select(g=>$"{g.Key}: {g.Count()}")));return;}
  await db.Database.MigrateAsync();Require(!await db.Users.AnyAsync()&&!await db.Documentos.AnyAsync(),409,"La importación exige una base vacía; no sobrescribe información existente.");await using var tx=await db.Database.BeginTransactionAsync();var s=services.GetRequiredService<IStore>();
  foreach(var d in docs.OrderBy(d=>d.S("kind")=="users"?0:1)){var j=JsonNode.Parse(d.S("data"))!.AsObject();j["id"]=d.S("id");j.Remove("_v");if(d.S("kind")=="users"&&j.S("role")=="Usuario")j["role"]="Solicitante";await s.Save(d.S("kind"),j);if(d.I("version",1)>1){if(d.S("kind")=="users"){var user=await db.Users.SingleAsync(x=>x.Id==d.S("id"));user.Version=d.I("version");}else{var entity=await db.Documentos.SingleAsync(x=>x.Kind==d.S("kind")&&x.Id==d.S("id"));entity.Version=d.I("version");}await db.SaveChangesAsync();db.ChangeTracker.Clear();}}
  foreach(var seq in root.A("sequences").Items())db.Secuencias.Add(new(){Name=seq.S("name"),Value=(long)seq.D("value")});
  foreach(var file in root.A("files").Items())db.Archivos.Add(new(){Key=file.S("key"),Name=file.S("name"),Type=file.S("type"),Content=Convert.FromBase64String(file.S("data"))});
  await db.SaveChangesAsync();await tx.CommitAsync();Console.WriteLine($"Importados {docs.Count} registros. Sesiones y secretos de alojamiento no se importan.");
 }
}
