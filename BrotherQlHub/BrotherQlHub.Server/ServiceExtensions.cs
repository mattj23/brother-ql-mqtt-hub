using BrotherQlHub.Data;
using Microsoft.EntityFrameworkCore;

namespace BrotherQlHub.Server;

public static class ServiceExtensions
{
    public static void UseDatabase(this IServiceCollection services, IConfigurationSection config)
    {
        var typeName = config.GetValue<string>("Type").ToLower();
        var connectionString = config.GetValue<string>("ConnectionString");
        switch (typeName)
        {
            case "sqlite":
                services.AddDbContext<HubContext>(o => o.UseSqlite(connectionString,
                    x => x.MigrationsAssembly("BrotherQlHub.Migrations.Sqlite")));
                break;
            // case "postgresql":
            //     services.AddDbContext<HubContext>(o => o.UseNpgsql(connectionString,
            //         x => x.MigrationsAssembly("BrotherQlHub.Migrations.PostgreSQL")));
            //     break;
            // case "mysql":
            //     services.AddDbContext<HubContext>(o => o.UseMySql(connectionString,
            //         ServerVersion.AutoDetect(connectionString), 
            //         x => x.MigrationsAssembly("BrotherQlHub.Migrations.MySQL")));
            //     break;
            default:
                throw new NotSupportedException($"No database provider found for type={typeName}");
        }
    }

    /// <summary>
    /// Applies database migrations to the application's data provider, which will create the database if it does
    /// not exist or update it if it's on an old version.
    /// </summary>
    /// <param name="app"></param>
    /// <exception cref="ApplicationException">thrown if no database context could be retrieved</exception>
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var services = app.ApplicationServices.CreateScope();
        var context = services.ServiceProvider.GetService<HubContext>();
        if (context is null) throw new ApplicationException("Attempted database migration with no context available");
        context.Database.Migrate();
    }
}