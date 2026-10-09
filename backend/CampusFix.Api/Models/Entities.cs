using Microsoft.AspNetCore.Identity;
namespace CampusFix.Api.Models;
public class Usuario : IdentityUser {
 public string Profile { get; set; } = "{}";
 public int Version { get; set; } = 1;
}
public class Documento {
 public string Kind { get; set; } = "";
 public string Id { get; set; } = "";
 public string Data { get; set; } = "{}";
 public int Version { get; set; } = 1;
}
public class Secuencia { public string Name { get; set; } = ""; public long Value { get; set; } }
public class Archivo { public string Key { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = ""; public byte[] Content { get; set; } = []; }
