using Microsoft.Extensions.DependencyInjection;
using Service.IService;
using Service.Service;

namespace Service
{
    /// <summary>Registers the Retail POS services. Call builder.Services.AddRetailPosServices2() once in MauiProgram.cs.</summary>
    public static class RetailPosServiceResolution2
    {
        public static IServiceCollection AddRetailPosServices2(this IServiceCollection services)
        {
            services.AddScoped<IRetailPosClientService2, RetailPosClientService2>();
            services.AddScoped<IRetailPosSaleService2, RetailPosSaleService2>();
            services.AddScoped<IRetailPosPaymentService2, RetailPosPaymentService2>();
            services.AddScoped<IRetailPosReportService2, RetailPosReportService2>();
            return services;
        }
    }
}
