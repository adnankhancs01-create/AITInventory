using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface ILogRepository
    {
        Task LogExceptionAsync(Exception ex, string applicationName = "AITInventory", int? userId = null,
            string additionalData = null, string? request = null);
    }
}
