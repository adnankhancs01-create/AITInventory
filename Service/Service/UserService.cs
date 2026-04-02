using Common;
using Common.Helpers;
using Data.Repositories;
using Domain.Entities;
using Domain.IRepositories;
using Service.IService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;

        public UserService(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<BaseResponse<object>> LoginAsync(string email, string password, bool isApiUser = true)
        {
            var result = await _userRepo.LoginAsync(email, password, isApiUser);
            var response = (AuthResponse)result;
            if (result is null || !response.Success)
            {
                return BaseResponse<object>.FailureResponse(
                    new List<string> { "Login failed" },
                    "User not found or invalid credentials"
                );
            }

            // Assuming UserRepo returns ApplicationUserIdentity on success
            return BaseResponse<object>.SuccessResponse(
                (object)result,
                "Login successful"
            );
        }
        public async Task<BaseResponse<object>> RegisterAsync(string userName, string email, string password,string mobileNo=null)
        {
            if(!email.IsValidEmail())
                return BaseResponse<object>.FailureResponse(
                    new List<string> { "Invalid email"},
                    "Unable to register user"
                );

            var result = await _userRepo.RegisterAsync(userName,email, password, mobileNo);
            var response = (AuthResponse)result;
            if (response is null || !response.Success)
                return BaseResponse<object>.FailureResponse(
                    new List<string> {response.Message },
                    "Unable to register user"
                );

            return BaseResponse<object>.SuccessResponse(
                (object)result,
                "User registered successfully"
            );
        }

        public async Task LogoutAsync()
        {
            await _userRepo.LogoutAsync();
        }
    }
}