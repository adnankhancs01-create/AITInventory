using Common;
using Common.RetailPos2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.RetailPos2;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    public class RetailPosClientService2 : IRetailPosClientService2
    {
        private readonly IRetailPosUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public RetailPosClientService2(IRetailPosUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
            _currentUser = currentUser;
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        public Task<BaseResponse<List<RetailPosClientRowDto2>>> GetClientsAsync(RetailPosClientFilterDto2 filter)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<List<RetailPosClientRowDto2>>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var rows = await uow.Clients.GetClientRowsAsync(filter ?? new RetailPosClientFilterDto2());
                return RetailPosResult2.Ok(rows);
            }, _logRepo, nameof(GetClientsAsync), _currentUser.UserId, filter);
        }

        public Task<BaseResponse<RetailPosClientDto2>> GetClientAsync(int clientId)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<RetailPosClientDto2>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var c = await uow.Clients.FindClientAsync(clientId);
                if (c == null) return RetailPosResult2.Fail<RetailPosClientDto2>("Client not found.");

                return RetailPosResult2.Ok(new RetailPosClientDto2
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Address = c.Address,
                    OpeningBalance = c.OpeningBalance,
                    CreditLimit = c.CreditLimit,
                    IsActive = c.IsActive,
                    CreatedOn = c.CreatedOn
                });
            }, _logRepo, nameof(GetClientAsync), _currentUser.UserId, new { clientId });
        }

        public Task<BaseResponse<int>> SaveClientAsync(RetailPosClientDto2 dto)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<int>(_currentUser, ScreenCatalog2.RetailPosClients);
                if (denied != null) return denied;

                int userId = _currentUser.UserId!.Value;
                bool isNew = dto.Id <= 0;
                var errors = new List<string>();

                string name = (dto.Name ?? string.Empty).Trim();
                string? phone = Clean(dto.Phone, 30);
                string? address = Clean(dto.Address, 300);
                decimal opening = RetailPosRules2.Round(dto.OpeningBalance);
                decimal? limit = dto.CreditLimit.HasValue ? RetailPosRules2.Round(dto.CreditLimit.Value) : null;

                if (name.Length == 0) errors.Add("Client name is required.");
                if (name.Length > 150) errors.Add("Client name can be at most 150 characters.");
                if ((dto.Phone?.Trim().Length ?? 0) > 30) errors.Add("Phone can be at most 30 characters.");
                if ((dto.Address?.Trim().Length ?? 0) > 300) errors.Add("Address can be at most 300 characters.");
                if (opening < 0m) errors.Add("Opening balance cannot be negative.");
                if (opening > RetailPosConstants2.MaxAmount) errors.Add("Opening balance is too large.");
                if (limit.HasValue && limit.Value < 0m) errors.Add("Credit limit cannot be negative.");
                if (limit.HasValue && limit.Value > RetailPosConstants2.MaxAmount) errors.Add("Credit limit is too large.");
                if (errors.Count > 0) return RetailPosResult2.Fail<int>(errors);

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                RetailPosClient2? client;
                if (isNew)
                {
                    if (!_currentUser.IsRoot && (opening != 0m || limit.HasValue))
                        return RetailPosResult2.Fail<int>("Only the business owner can set an opening balance or a credit limit.");

                    client = new RetailPosClient2 { CreatedBy = userId, CreatedOn = DateTime.Now, OpeningBalance = opening, CreditLimit = limit };
                }
                else
                {
                    client = await uow.Clients.GetClientAsync(dto.Id);
                    if (client == null) return RetailPosResult2.Fail<int>("Client not found.");

                    if (opening != client.OpeningBalance)
                        return RetailPosResult2.Fail<int>("The opening balance cannot be edited after the client is created. Ask the owner to record a balance adjustment instead.");
                    if (limit != client.CreditLimit && !_currentUser.IsRoot)
                        return RetailPosResult2.Fail<int>("Only the business owner can change a client's credit limit.");

                    client.CreditLimit = limit;
                    client.ModifiedOn = DateTime.Now;
                    client.ModifiedBy = userId;
                }

                if (await uow.Clients.ClientExistsAsync(name, phone, isNew ? 0 : client.Id))
                    return RetailPosResult2.Fail<int>("A client with the same name and phone already exists.");

                client.Name = name;
                client.Phone = phone;
                client.Address = address;
                client.IsActive = dto.IsActive;

                if (isNew) uow.Clients.AddClient(client);

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return RetailPosResult2.Ok(client.Id, "Client saved.");
            }, _logRepo, nameof(SaveClientAsync), _currentUser.UserId, dto);
        }
    }
}
