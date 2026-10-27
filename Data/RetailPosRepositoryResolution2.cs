using Data.Repositories;
using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    /// <summary>Registers the Retail POS repositories. Call builder.Services.AddRetailPosRepositories2() once in MauiProgram.cs.</summary>
    public static class RetailPosRepositoryResolution2
    {
        public static IServiceCollection AddRetailPosRepositories2(this IServiceCollection services)
        {
            services.AddScoped<IRetailPosClientRepo2, RetailPosClientRepo2>();
            services.AddScoped<IRetailPosSaleRepo2, RetailPosSaleRepo2>();
            services.AddScoped<IRetailPosPaymentRepo2, RetailPosPaymentRepo2>();
            services.AddScoped<IRetailPosUnitOfWorkFactory2, RetailPosUnitOfWorkFactory2>();
            return services;
        }
    }
}
