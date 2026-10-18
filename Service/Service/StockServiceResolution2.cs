using Microsoft.Extensions.DependencyInjection;
using Service.IService;

namespace Service.Service
{
    /// <summary>Registers every Stock2 service. Call builder.Services.AddStockServices2() once in MauiProgram.cs.</summary>
    public static class StockServiceResolution2
    {
        public static IServiceCollection AddStockServices2(this IServiceCollection services)
        {
            // Scoped like the existing services. Each service opens its own DI scope per operation,
            // so the long-lived MAUI DbContext is never shared with the Stock2 transactions.
            services.AddScoped<IStockMasterService2, StockMasterService2>();
            services.AddScoped<IStockQueryService2, StockQueryService2>();
            services.AddScoped<IStockReceivingService2, StockReceivingService2>();
            services.AddScoped<IStockIssueService2, StockIssueService2>();
            services.AddScoped<IStockTransferService2, StockTransferService2>();
            services.AddScoped<IStockAdjustmentService2, StockAdjustmentService2>();
            return services;
        }
    }
}
