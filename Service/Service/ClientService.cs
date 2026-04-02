using AutoMapper;
using Common;
using Common.Helpers;
using Common.Models;
using Common.Models.RequestModel;
using Data.Repositories;
using Domain.Entities;
using Domain.IRepositories;
using Service.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Service
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILogRepository _logRepo;
        private readonly IMapper _mapper;
        public ClientService(
            IClientRepository clientRepository,
            ILogRepository logRepo,
            IMapper mapper)
        {
            _clientRepository = clientRepository;
            _logRepo = logRepo;
            _mapper = mapper;
        }
        public async Task<BaseResponse<int>> AddEditClientAsync(AddEditClientDetailRequestModel model)
        {
            try
            {
                if (model == null)
                    return BaseResponse<int>.FailureResponse(new List<string>() { "invalid request" });

                if (string.IsNullOrWhiteSpace(model.FirstName))
                    return BaseResponse<int>.FailureResponse(new List<string>() { "First Name is required" });

                if (!string.IsNullOrWhiteSpace(model.Email) && !model.Email.IsValidEmail())
                    return BaseResponse<int>.FailureResponse(new List<string>() { "Invalid Email" });

                return await _clientRepository.AddEditClientAsync(model);

            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex);
                return BaseResponse<int>.FailureResponse(new List<string>() { "something went wrong" });
            }
        }

        public async Task<BaseResponse<(List<ClientModel>, int)>> GetAllClientsAsync(int id, int pageIndex, int pageSize, string? Filter)
        {
            try
            {
                var result = await _clientRepository.GetAllClientsAsync(id, pageIndex, pageSize,Filter);

                var data = _mapper.Map<List<VendorClientDetail>, List<ClientModel>>(result.Item1);
                var totalCount = result.Item2;

                return BaseResponse<(List<ClientModel>, int)>.SuccessResponse((data, totalCount));
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex);
                return BaseResponse<(List<ClientModel>, int)>.FailureResponse(new List<string>() { "something went wrong" });
            }
        }

    }
}
