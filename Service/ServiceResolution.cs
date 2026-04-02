using Service.Services;
using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Service.IService;
using Service.Service;

namespace Service
{
    public static class ServiceResolution
    {
        public static IServiceCollection AddServiceResolution(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IClientService, ClientService>();

            return services;
        }
    }
}
