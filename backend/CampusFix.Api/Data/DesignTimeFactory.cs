using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CampusFix.Api.Data;
public class DesignTimeFactory:IDesignTimeDbContextFactory<CampusFixDbContext>{
 public CampusFixDbContext CreateDbContext(string[] args){var config=new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json",optional:true).AddUserSecrets<Program>(optional:true).AddEnvironmentVariables().Build();return new(new DbContextOptionsBuilder<CampusFixDbContext>().UseNpgsql(config.GetConnectionString("CampusFix")??"Host=localhost;Database=campusfix;Username=campusfix").Options);}
}
