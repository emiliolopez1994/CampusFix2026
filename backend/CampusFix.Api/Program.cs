using System.Text;
using CampusFix.Api.Data;
using CampusFix.Api.Models;
using CampusFix.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
var builder=WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=600000);
var connection=builder.Configuration.GetConnectionString("CampusFix")??"Host=localhost;Port=5432;Database=campusfix;Username=campusfix";
builder.Services.AddDbContext<CampusFixDbContext>(o=>o.UseNpgsql(connection));
builder.Services.AddIdentityCore<Usuario>(o=>{o.Password.RequiredLength=12;o.Password.RequireDigit=true;o.Password.RequireUppercase=true;o.Password.RequireLowercase=true;o.Password.RequireNonAlphanumeric=false;o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);}).AddRoles<IdentityRole>().AddEntityFrameworkStores<CampusFixDbContext>();
builder.Services.AddScoped<IStore,Store>();builder.Services.AddScoped<CampusService>();
builder.Services.AddHttpClient("mail",c=>c.Timeout=TimeSpan.FromSeconds(20));
var jwtKey=builder.Configuration["Jwt:Key"];
// Design-time EF operations and import do not issue sessions.
var command=args.Contains("--import")||args.Contains("--schema")||args.Contains("--check-data");
if(string.IsNullOrWhiteSpace(jwtKey)||jwtKey.Length<32){if(!command&&Environment.GetEnvironmentVariable("CAMPUSFIX_DESIGN_TIME")!="1")throw new InvalidOperationException("Configura Jwt:Key con una clave aleatoria de al menos 32 caracteres. Consulta INSTALACION.md.");jwtKey=new string('x',64);}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>{o.MapInboundClaims=false;o.TokenValidationParameters=new(){ValidateIssuer=true,ValidateAudience=true,ValidateIssuerSigningKey=true,ValidateLifetime=true,ValidIssuer="CampusFix.Api",ValidAudience="CampusFix.Angular",IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),ClockSkew=TimeSpan.FromSeconds(30)};});
builder.Services.AddAuthorization();builder.Services.AddControllers();builder.Services.AddOpenApi();
var app=builder.Build();
if(command){await using var scope=app.Services.CreateAsyncScope();await Importer.Command(args,scope.ServiceProvider);return;}
app.Use(async(context,next)=>{try{await next();}catch(ApiError e){context.Response.StatusCode=e.Status;await context.Response.WriteAsJsonAsync(new{title=e.Message,errors=e.Field==null?null:new Dictionary<string,string[]>{{e.Field,new[]{e.Message}}}});}catch(DbUpdateConcurrencyException){context.Response.StatusCode=409;await context.Response.WriteAsJsonAsync(new{title="El registro cambió en otra sesión. Actualiza antes de guardar."});}catch(Exception e){app.Logger.LogError(e,"Error de operación CampusFix. Referencia {TraceId}",context.TraceIdentifier);context.Response.StatusCode=500;await context.Response.WriteAsJsonAsync(new{title="No se pudo guardar la operación. Consulta tus reportes antes de reintentar y comunica la referencia a soporte.",traceId=context.TraceIdentifier});}});
app.UseDefaultFiles();app.UseStaticFiles();app.UseAuthentication();app.UseAuthorization();app.MapControllers();
if(app.Environment.IsDevelopment())app.MapOpenApi();
app.Map("/api/{**path}",()=>Results.NotFound(new{title="Ruta no encontrada."}));
app.MapFallbackToFile("{*path:nonfile}","index.html");
app.Run();
public partial class Program { }
