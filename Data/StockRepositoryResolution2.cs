using Data.Repositories;
using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    /// <summary>Registers the Stock2 repositories and unit of work. Call builder.Services.AddStockRepositories2() once in MauiProgram.cs.</summary>
    public static class StockRepositoryResolution2
    {
        public static IServiceCollection AddStockRepositories2(this IServiceCollection services)
        {
            services.AddScoped<IStockLedgerRepo2, StockLedgerRepo2>();
            services.AddScoped<IStockMasterRepo2, StockMasterRepo2>();
            services.AddScoped<IStockReceivingRepo2, StockReceivingRepo2>();
            services.AddScoped<IStockIssueRepo2, StockIssueRepo2>();
            services.AddScoped<IStockTransferRepo2, StockTransferRepo2>();
            services.AddScoped<IStockAdjustmentRepo2, StockAdjustmentRepo2>();
            services.AddScoped<IStockReportRepo2, StockReportRepo2>();
            services.AddScoped<IStockUnitOfWorkFactory2, StockUnitOfWorkFactory2>();
            return services;
        }
    }
}
