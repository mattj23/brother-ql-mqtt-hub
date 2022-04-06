using BrotherQlHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BrotherQlHub.Migrations.Mysql;

public class MigrationContextFactory : IDesignTimeDbContextFactory<HubContext>
{
    public HubContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<HubContext>();
        builder.UseMySql(new MySqlServerVersion(new Version(8, 0)),
            x => x.MigrationsAssembly("BrotherQlHub.Migrations.Mysql"));
        return new HubContext(builder.Options);
    }
}