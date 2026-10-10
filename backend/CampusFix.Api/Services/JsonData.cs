using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace CampusFix.Api.Services;
public class ApiError(int status,string message,string? field=null):Exception(message) { public int Status {get;}=status; public string? Field {get;}=field; }
public static class J {
 public static JsonObject Obj(object? o=null)=>o is null?new():JsonSerializer.SerializeToNode(o)!.AsObject();
 public static JsonArray Arr(IEnumerable<JsonObject> xs)=>new(xs.Select(x=>(JsonNode)x.DeepClone()).ToArray());
 public static string S(this JsonNode? x,string k,string fallback="")=>x?[k]?.ToString()??fallback;
 public static bool B(this JsonNode? x,string k,bool fallback=false)=>bool.TryParse(x?[k]?.ToString(),out var n)?n:fallback;
 public static int I(this JsonNode? x,string k,int fallback=0)=>int.TryParse(x?[k]?.ToString(),out var n)?n:fallback;
 public static double D(this JsonNode? x,string k,double fallback=0)=>double.TryParse(x?[k]?.ToString(),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var n)?n:fallback;
 public static JsonObject O(this JsonNode? x,string k)=>x?[k] as JsonObject??new();
 public static JsonArray A(this JsonNode? x,string k)=>x?[k] as JsonArray??new();
 public static IEnumerable<JsonObject> Items(this JsonArray x)=>x.OfType<JsonObject>();
 public static bool Has(this JsonArray x,string s)=>x.Any(n=>n?.ToString()==s);
 public static JsonObject Copy(this JsonObject x)=>(JsonObject)x.DeepClone();
 public static JsonObject Merge(JsonObject a,JsonObject b){var c=a.Copy();foreach(var kv in b)c[kv.Key]=kv.Value?.DeepClone();return c;}
 public static string Id()=>Guid.NewGuid().ToString();
 public static string Now()=>DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
 public static DateTimeOffset Date(string s)=>DateTimeOffset.TryParse(s,out var d)?d:DateTimeOffset.MinValue;
 public static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool c,int status,string message){if(!c)throw new ApiError(status,message);}
 public static string Required(JsonObject b,string key,string label,int max=160){var v=b.S(key).Trim();if(v.Length==0||v.Length>max)throw new ApiError(400,$"{label}: completa el campo (máximo {max} caracteres).",key);return v;}
 public static string Email(JsonObject b,string key="correo"){var s=Required(b,key,"Correo",254).ToLowerInvariant();Require(Regex.IsMatch(s,@"^[^\s@]+@[^\s@]+\.[^\s@]+$"),400,"Correo no válido.");return s;}
 public static string Password(JsonObject b,string key="password"){var s=b.S(key);Require(s.Length>=12&&s.Length<=128&&Regex.IsMatch(s,"[a-z]")&&Regex.IsMatch(s,"[A-Z]")&&Regex.IsMatch(s,"[0-9]"),400,"Usa de 12 a 128 caracteres, mayúsculas, minúsculas y números.");return s;}
 public static void Version(JsonObject? old,JsonObject b){if(b.ContainsKey("version")&&b["version"]!=null)Require(old!=null&&b.I("version",-1)==old.I("_v"),409,"El registro cambió. Actualiza antes de guardar.");}
 public static JsonArray Strings(JsonNode? n,string name){Require(n is JsonArray a&&a.Count<=200&&a.All(v=>v is JsonValue j&&j.TryGetValue<string>(out _)),400,name+": selecciona una lista válida.");return new JsonArray(((JsonArray)n!).Select(v=>v!.ToString()).Distinct().Select(v=>(JsonNode?)JsonValue.Create(v)).ToArray());}
}
