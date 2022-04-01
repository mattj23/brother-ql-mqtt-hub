using BrotherQlHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BrotherQlHub.Migrations.Sqlite;

public class MigrationContextFactory : IDesignTimeDbContextFactory<HubContext>
{
    public HubContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<HubContext>();
        builder.UseSqlite(x => x.MigrationsAssembly("BrotherQlHub.Migrations.Sqlite"));
        return new HubContext(builder.Options);
    }
}