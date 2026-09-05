using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiAlreadyDidIt.Api.Data;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without booting the web host.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AadiDbContext>
{
    public AadiDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AadiDbContext>();
        optionsBuilder
            .UseNpgsql(configuration.GetConnectionString("AiAlreadyDidIt"),
                b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName).UseVector())
            .UseSnakeCaseNamingConvention();

        return new AadiDbContext(optionsBuilder.Options);
    }
}
