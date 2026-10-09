using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
namespace CampusFix.Api.Services;
public static class BusinessCalendar {
 public static string Due(string start,double hours,JsonObject? calendar){Require(double.IsFinite(hours)&&hours>0&&hours<=8760,400,"Horas de atención no válidas.");var d=Date(start);Require(d!=DateTimeOffset.MinValue,400,"Fecha no válida.");if(calendar==null)return d.AddHours(hours).ToString("O");
  TimeZoneInfo tz;try{tz=TimeZoneInfo.FindSystemTimeZoneById(calendar.S("timezone","America/Guayaquil"));}catch{throw new ApiError(400,"Zona horaria no válida.");}
  Require(TimeOnly.TryParse(calendar.S("start","08:00"),out var begin)&&TimeOnly.TryParse(calendar.S("end","17:00"),out _) ,400,"Horario no válido.");var end=TimeOnly.Parse(calendar.S("end","17:00"));var days=calendar.A("days");Require(days.Count>0&&begin<end,400,"Calendario no válido.");d=DateTimeOffset.FromUnixTimeSeconds((long)Math.Ceiling(d.ToUnixTimeMilliseconds()/60000d)*60);var limit=d.AddDays(366);var remaining=(int)Math.Ceiling(hours*60);
  while(remaining>0&&d<limit){var local=TimeZoneInfo.ConvertTime(d,tz);var time=TimeOnly.FromDateTime(local.DateTime);if(days.Any(x=>x?.ToString()==((int)local.DayOfWeek).ToString())&&!calendar.A("holidays").Has(local.ToString("yyyy-MM-dd"))&&time>=begin&&time<end)remaining--;d=d.AddMinutes(1);}
  Require(remaining==0,400,"La meta excede un año del calendario.");return d.ToString("O");
 }
}
