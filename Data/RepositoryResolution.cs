using Data.Repositories;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    public static class RepositoryResolution
    {
        public static IServiceCollection AddRepositoryResolution(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IProductRepo, ProductRepo>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IClientRepository, ClientRepository>();

            return services;
        }
    }
}
