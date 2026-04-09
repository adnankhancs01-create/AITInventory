using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface ITransactionRepo
    {
        Task<VendorTransaction> AddTransactionAsync(VendorTransaction transaction);
        string GenerateSlip(VendorTransaction transaction);
    }
}
