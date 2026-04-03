using Common;
using Common.Helpers;
using Common.Models.RequestModel;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Dynamic.Core;

namespace Data.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly InventoryDbContext _dbContext;
        public ClientRepository(InventoryDbContext inventoryDb)
        {
            _dbContext = inventoryDb;
        }
        public async Task<BaseResponse<int>> AddEditClientAsync(AddEditClientDetailRequestModel model)
        {
            VendorClientDetail entity;

            if (model.Id > 0)
            {
                // 🔹 UPDATE
                entity = await _dbContext.VendorClientDetail.FirstOrDefaultAsync(x => x.Id == model.Id);

                if (entity == null)
                    return BaseResponse<int>.FailureResponse(new List<string>() { "client not found" });

                entity.FirstName = model.FirstName;
                entity.LastName = model.LastName;
                entity.Email = model.Email;
                entity.Phone = model.Phone;
                entity.Address = model.Address;
                entity.City = model.City;
                entity.State = model.State;
                entity.PostalCode = model.PostalCode;
                entity.ModifiedOn = DateTime.UtcNow;
                entity.Country = model.Country;
                entity.IsActive = model.IsActive ?? true;
            }
            else
            {
                // 🔹 INSERT
                entity = new VendorClientDetail
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    PostalCode = model.PostalCode,
                    Country = model.Country,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    ClientCode= await GetClientCodeAsync()
                };

                await _dbContext.VendorClientDetail.AddAsync(entity);
            }

            await _dbContext.SaveChangesAsync();

            return BaseResponse<int>.SuccessResponse(entity.Id, model.Id > 0 ? "Client updated successfully" : "Client added successfully");
        }

        private async Task<string> GetClientCodeAsync()
        {
            var getClient=await _dbContext.VendorClientDetail.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if(getClient == null || string.IsNullOrEmpty(getClient.ClientCode))
                return "CLT-0001";

            return $"CLT-{(int.Parse(getClient.ClientCode.Split('-')[1]) + 1).ToString("D4")}";

        }

        public async Task<(List<VendorClientDetail>,int count)> GetAllClientsAsync(int id, int pageIndex, int pageSize,string? Filter)
        {
            var query = _dbContext.VendorClientDetail
                .Where(x => x.Id == id || id == 0);

            if (!string.IsNullOrEmpty(Filter))
                query=query.Where(Filter);

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data,totalCount);

        }
    }
}
