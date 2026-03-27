using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class LogRepository: ILogRepository
    {
        private readonly InventoryDbContext _dbContext;
        public LogRepository(InventoryDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task LogExceptionAsync(Exception ex, string applicationName = "AITInventory", int? userId = null, string additionalData = null)
        {
            if (ex == null) return;

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
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ExceptionLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
    }
}
