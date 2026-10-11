using System.Text.Json.Nodes;
using CampusFix.Api.Data;
using CampusFix.Api.Models;
using CampusFix.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusFix.Api.Controllers;
[ApiController]
[Route("api")]
public class CampusController(CampusService service,CampusFixDbContext db):ControllerBase {
 [HttpGet("health")]
 public async Task<IActionResult> Health(){var connected=await db.Database.CanConnectAsync();return StatusCode(connected?200:503,new{estado=connected?"Conectado":"Sin conexión",baseDatos="PostgreSQL",frontend="Angular",backend="ASP.NET Core"});}
 [HttpGet("auth/me")]
 public Task<IActionResult> Operation0()=>Run("auth/me");
 [HttpPost("auth/login")]
 public Task<IActionResult> Operation1([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("auth/login",body);
 [HttpPost("auth/registro")]
 public Task<IActionResult> Operation2([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("auth/registro",body);
 [HttpPost("auth/logout")]
 public Task<IActionResult> Operation3([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("auth/logout",body);
 [HttpPost("auth/password")]
 public Task<IActionResult> Operation4([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("auth/password",body);
 [HttpGet("data")]
 public Task<IActionResult> Operation5()=>Run("data");
 [HttpGet("usuarios")]
 public Task<IActionResult> Operation6()=>Run("usuarios");
 [HttpPost("usuarios")]
 public Task<IActionResult> Operation7([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("usuarios",body);
 [HttpPut("usuarios/{id}")]
 public Task<IActionResult> Operation8(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"usuarios/{id}",body);
 [HttpPut("usuarios/{id}/reset")]
 public Task<IActionResult> Operation9(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"usuarios/{id}/reset",body);
 [HttpGet("permisos")]
 public Task<IActionResult> Operation10()=>Run("permisos");
 [HttpPut("permisos")]
 public Task<IActionResult> Operation11([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("permisos",body);
 [HttpPost("config")]
 public Task<IActionResult> Operation12([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("config",body);
 [HttpPost("config/template")]
 public Task<IActionResult> Operation13([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("config/template",body);
 [HttpGet("reportes")]
 public Task<IActionResult> Operation14()=>Run("reportes");
 [HttpPost("reportes")]
 public Task<IActionResult> Operation15([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("reportes",body);
 [HttpGet("reportes/{id}")]
 public Task<IActionResult> Operation16(string id)=>Run($"reportes/{id}");
 [HttpPut("reportes/{id}")]
 public Task<IActionResult> Operation17(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"reportes/{id}",body);
 [HttpDelete("reportes/{id}")]
 public Task<IActionResult> Operation18(string id)=>Run($"reportes/{id}");
 [HttpPut("reportes/{id}/asignar")]
 public Task<IActionResult> Operation19(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"reportes/{id}/asignar",body);
 [HttpPatch("reportes/{id}/estado")]
 public Task<IActionResult> Operation20(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"reportes/{id}/estado",body);
 [HttpPut("reportes/{id}/solucion")]
 public Task<IActionResult> Operation21(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"reportes/{id}/solucion",body);
 [HttpPost("reportes/{id}/mensajes")]
 public Task<IActionResult> Operation22(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"reportes/{id}/mensajes",body);
 [HttpPut("reportes/{id}/valoracion")]
 public Task<IActionResult> Operation23(string id,[FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run($"reportes/{id}/valoracion",body);
 [HttpGet("archivo")]
 public Task<IActionResult> Operation24()=>Run("archivo");
 [HttpGet("notifications")]
 public Task<IActionResult> Operation25()=>Run("notifications");
 [HttpPut("notifications")]
 public Task<IActionResult> Operation26([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("notifications",body);
 [HttpPost("articles")]
 public Task<IActionResult> Operation27([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("articles",body);
 [HttpPost("votes")]
 public Task<IActionResult> Operation28([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("votes",body);
 [HttpGet("analytics")]
 public Task<IActionResult> Operation29()=>Run("analytics");
 [HttpGet("mail")]
 public Task<IActionResult> Operation30()=>Run("mail");
 [HttpPut("mail")]
 public Task<IActionResult> Operation31([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("mail",body);
 [HttpPost("mail/test")]
 public Task<IActionResult> Operation32([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("mail/test",body);
 [HttpPost("mail/activate")]
 public Task<IActionResult> Operation33([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("mail/activate",body);
 [HttpPost("mail/process")]
 public Task<IActionResult> Operation34([FromBody(EmptyBodyBehavior=Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject? body)=>Run("mail/process",body);
 [HttpGet("auth/areas")]
 public Task<IActionResult> RegistrationAreas()=>Run("auth/areas");
 [HttpPut("auth/profile")]
 public Task<IActionResult> Profile([FromBody] JsonObject body)=>Run("auth/profile",body);
 [HttpPut("reportes/{id}/clasificar")]
 public Task<IActionResult> Classify(string id,[FromBody] JsonObject body)=>Run($"reportes/{id}/clasificar",body);
 [HttpDelete("config/{id}")]
 public Task<IActionResult> DeleteConfig(string id,[FromBody] JsonObject body)=>Run($"config/{id}",body);
 [HttpDelete("usuarios/{id}")]
 public Task<IActionResult> DeleteUser(string id,[FromBody] JsonObject body)=>Run($"usuarios/{id}",body);
 [HttpDelete("articles/{id}")]
 public Task<IActionResult> DeleteArticle(string id,[FromBody] JsonObject body)=>Run($"articles/{id}",body);
 [HttpDelete("mail")]
 public Task<IActionResult> DeleteMail([FromBody] JsonObject body)=>Run("mail",body);
 [HttpGet("permission-profiles")]
 public Task<IActionResult> PermissionProfiles()=>Run("permission-profiles");
 [HttpPost("permission-profiles"),HttpPut("permission-profiles"),HttpDelete("permission-profiles")]
 public Task<IActionResult> SavePermissionProfile([FromBody] JsonObject body)=>Run("permission-profiles",body);
 [HttpPut("reportes/{id}/mensajes/{messageId}")]
 public Task<IActionResult> EditMessage(string id,string messageId,[FromBody] JsonObject body)=>Run($"reportes/{id}/mensajes/{messageId}",body);
 async Task<IActionResult> Run(string path,JsonObject? body=null){
  Response.Headers.CacheControl="no-store";Response.Headers.XContentTypeOptions="nosniff";
  // Atomic business changes: ticket, history, attachment and notifications commit together.
  // Login persists the failed-attempt counter even when it responds with 401.
  var transactional=path!="auth/login";
  await using var transaction=transactional?await db.Database.BeginTransactionAsync():null;
  // Serialize administrative/reference writes so dependency and last-owner checks
  // cannot race another create, reassignment or soft deletion.
  if(transactional&&Request.Method!="GET"&&(path.StartsWith("usuarios")||path.StartsWith("config")||path.StartsWith("reportes")||path.StartsWith("articles")||path=="mail"||path=="auth/registro"||path=="auth/profile"))
   await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261010,41)");
  var result=await service.Run(path.Trim('/'),Request.Method,body??new(),User,Request.Query);
  if(transaction!=null)await transaction.CommitAsync();
  if(result is Archivo file)return File(file.Content,file.Type,file.Name);
  return StatusCode(Request.Method=="POST"&&path is "reportes" or "usuarios" or "auth/registro"?201:200,result);
 }
}
