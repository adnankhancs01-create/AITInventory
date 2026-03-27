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

            return services;
        }
    }
}
