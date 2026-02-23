using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheRealBank.Contexts;

namespace TheRealBank.Repositories
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDesignerRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("DefaultConnection") ?? configuration["ConnectionString"];
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

            var versionText = configuration["MySqlVersion"] ?? "8.0.36-mysql";

            services.AddDbContext<MainContext>(options =>
                options.UseMySql(cs, ServerVersion.Parse(versionText)));

            return services;
        }
    }
}