using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LogRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task LogExceptionAsync(Exception ex, string applicationName = "AITInventory", int? userId = null,
            string additionalData = null, string? request = null)
        {
            try
            {
                if (ex == null) return;

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var log = new ExceptionLog
                {
                    ApplicationName = applicationName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.ToString(),
                    Source = ex.Source,
                    MethodName = ex.TargetSite?.Name,
                    UserId = userId,
                    AdditionalData = additionalData,
                    CreatedAt = DateTime.UtcNow,
                    Request = request
                };

                await dbContext.ExceptionLogs.AddAsync(log);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Optional: fallback (file / console)
            }
        }
    }
}
