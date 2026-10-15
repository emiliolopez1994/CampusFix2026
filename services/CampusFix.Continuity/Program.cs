using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusFix.Continuity;
using Npgsql;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5149");
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=2_000_000);
var connection=new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Continuity")??"");
if(connection.Host is not "localhost" and not "127.0.0.1" || connection.Database is not "campusfix_continuity" && !(connection.Database??"").StartsWith("campusfix_continuity_qa"))throw new InvalidOperationException("Continuidad requiere su base PostgreSQL local independiente.");
connection.Timeout=5;connection.CommandTimeout=8;
builder.Services.AddSingleton(NpgsqlDataSource.Create(connection.ConnectionString));builder.Services.AddSingleton<Repository>();
var key=builder.Configuration["Continuity:ApiKey"];
if(string.IsNullOrWhiteSpace(key)||key.Length<32)throw new InvalidOperationException("Configura Continuity:ApiKey fuera del repositorio.");
var keyHash=SHA256.HashData(Encoding.UTF8.GetBytes(key));
var app=builder.Build();var repo=app.Services.GetRequiredService<Repository>();
if(args.Contains("--migrate")){await repo.Initialize();return;}
if(args.Contains("--seed-local")){
    var file=builder.Configuration["SeedFile"]??throw new InvalidOperationException("Falta SeedFile.");
    var areas=JsonSerializer.Deserialize<SeedArea[]>(await File.ReadAllTextAsync(file),Repository.Json)??[];
    await repo.Change(s=>Seed.Apply(s,areas,DateTimeOffset.UtcNow));return;
}
app.Use(async(ctx,next)=>{
    ctx.Response.Headers.CacheControl="no-store";ctx.Response.Headers.XContentTypeOptions="nosniff";
    if(ctx.Request.Path!="/health"&&!CryptographicOperations.FixedTimeEquals(keyHash,SHA256.HashData(Encoding.UTF8.GetBytes(ctx.Request.Headers["X-Continuity-Key"].ToString())))){ctx.Response.StatusCode=401;return;}
    try{await next();}catch(RuleError e){ctx.Response.StatusCode=e.Status;await ctx.Response.WriteAsJsonAsync(new{title=e.Message});}
    catch(Exception e) when(e is NpgsqlException or TimeoutException){app.Logger.LogError(e,"Continuidad PostgreSQL no disponible");ctx.Response.StatusCode=503;await ctx.Response.WriteAsJsonAsync(new{title="Continuidad académica no está disponible temporalmente."});}
});
app.MapGet("/health",async()=>{await repo.Read();return Results.Ok(new{service="CampusFix.Continuity",status="ready",database="PostgreSQL local independiente"});});
void Validate(Request r){Engine.Need(r.Scope!=null&&!string.IsNullOrWhiteSpace(r.Scope.Actor)&&r.Scope.Areas!=null&&r.Scope.Tickets!=null,"Contexto de acceso inválido.");}
app.MapPost("/v1/view",async(Request request)=>{Validate(request);return Engine.View(await repo.Read(),request.Scope,DateTimeOffset.UtcNow);});
app.MapPost("/v1/command",async(Request request)=>{Validate(request);Engine.Need(request.Command!=null,"Falta la operación.");var state=await repo.Change(s=>Engine.Apply(s,request.Scope,request.Command!,DateTimeOffset.UtcNow));return Engine.View(state,request.Scope,DateTimeOffset.UtcNow);});
app.Run();
