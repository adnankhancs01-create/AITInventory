using Common;
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

        public async Task<BaseResponse<object>> LoginAsync(string email, string password)
        {
            var result = await _userRepo.LoginAsync(email, password);

            if (result is null)
                return BaseResponse<object>.FailureResponse(
                    new List<string> { "Login failed" },
                    "User not found or invalid credentials"
                );

            // Assuming UserRepo returns ApplicationUserIdentity on success
            return BaseResponse<object>.SuccessResponse(
                (object)result,
                "Login successful"
            );
        }

        public async Task<BaseResponse<object>> RegisterAsync(string userName, string email, string password)
        {
            var result = await _userRepo.RegisterAsync(userName,email, password);

            if (result is null)
                return BaseResponse<object>.FailureResponse(
                    new List<string> { "Registration failed" },
                    "Unable to register user"
                );

            return BaseResponse<object>.SuccessResponse(
                (object)result,
                "User registered successfully"
            );
        }
    }
}