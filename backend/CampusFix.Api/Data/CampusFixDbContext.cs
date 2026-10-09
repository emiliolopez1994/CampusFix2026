using CampusFix.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace CampusFix.Api.Data;
public class CampusFixDbContext(DbContextOptions<CampusFixDbContext> options) : IdentityDbContext<Usuario>(options) {
 public DbSet<Documento> Documentos => Set<Documento>();
 public DbSet<Secuencia> Secuencias => Set<Secuencia>();
 public DbSet<Archivo> Archivos => Set<Archivo>();
 protected override void OnModelCreating(ModelBuilder b) {
  base.OnModelCreating(b);
  b.Entity<Usuario>().Property(x=>x.Profile).HasColumnType("jsonb");
  b.Entity<Usuario>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<Documento>().HasKey(x=>new{x.Kind,x.Id});
  b.Entity<Documento>().Property(x=>x.Data).HasColumnType("jsonb");
  b.Entity<Documento>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<Secuencia>().HasKey(x=>x.Name);
  b.Entity<Archivo>().HasKey(x=>x.Key);
 }
}
