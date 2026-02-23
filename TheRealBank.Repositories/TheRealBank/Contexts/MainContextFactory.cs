using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TheRealBank.Contexts
{
    public class MainContextFactory : IDesignTimeDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory(); 
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<MainContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("DefaultConnection")
                     ?? config["ConnectionString"]
                     ?? "server=localhost;port=3306;user=root;password=admin;database=aaaa";
            var versionText = config["MySqlVersion"] ?? "8.0.36-mysql";

            var options = new DbContextOptionsBuilder<MainContext>()
                .UseMySql(cs, ServerVersion.Parse(versionText));

            return new MainContext(options.Options);
        }
    }
}
