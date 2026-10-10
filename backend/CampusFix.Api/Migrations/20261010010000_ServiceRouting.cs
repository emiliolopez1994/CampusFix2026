using CampusFix.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CampusFix.Api.Migrations;

[DbContext(typeof(CampusFixDbContext))]
[Migration("20261010010000_ServiceRouting")]
public class ServiceRouting : Migration
{
    public static string Script(string name)
    {
        using var stream = typeof(ServiceRouting).Assembly.GetManifestResourceStream($"CampusFix.Api.Migrations.Sql.service-routing-{name}.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Script("up"));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Script("down"));
}
