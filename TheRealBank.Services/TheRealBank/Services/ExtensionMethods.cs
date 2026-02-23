using Microsoft.Extensions.DependencyInjection;
using TheRealBank.Services.Chat;
using TheRealBank.Services.Customers;

namespace TheRealBank.Services
{
    public static class ExtensionMethods
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<OllamaChatService>();
            return services;
        }
    }
}