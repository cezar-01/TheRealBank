using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheRealBank.Contexts;
using TheRealBank.Repositories.Users;

namespace TheRealBank.Repositories
{
    // Force MySQL at runtime (Razor Pages app)
    public static class ExtensionMethods
    {
        public static IServiceCollection AddDesignerRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("DefaultConnection") ?? configuration["ConnectionString"];
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection é obrigatório.");

            var versionText = configuration["MySqlVersion"] ?? "8.0.36-mysql";

            services.AddDbContext<MainContext>(options =>
                options.UseMySql(cs, ServerVersion.Parse(versionText)));

            services.AddScoped<ICustomerRepository, CustomerRepository>();
            return services;
        }
    }
}