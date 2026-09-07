using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class UserRepo : IUserRepo
    {
        private readonly InventoryDbContext _inventoryDbContext;

        public UserRepo(InventoryDbContext inventoryDbContext)
        {
            _inventoryDbContext = inventoryDbContext;
        }

        public async Task<object> LoginAsync(string email, string password, bool isApiUser = true)
        {
            var user = await _inventoryDbContext.UserMst
                .FirstOrDefaultAsync(x=> x.Email.Equals(email) && x.Password.Equals(password));

            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            return new AuthResponse { Success = true, Message = "Login success" };
        }

        public async Task<object> RegisterAsync(SignUpModel signUpModel)
        {
            await _inventoryDbContext.UserMst.AddAsync(new Domain.Entities.UserMst
            {
                Password = signUpModel.Password,
                Email = signUpModel.Email,
                mobileNo = signUpModel.MobileNo,
                UserName = signUpModel.UserName,
                BusinessName = signUpModel.BusinessName,
                BusinessAddress= signUpModel.BusinessAddress
            });
            await _inventoryDbContext.SaveChangesAsync();
            return new AuthResponse { Success = true, Message = "Registered" };
        }
        public async Task LogoutAsync()
        {
           //await _signInManager.SignOutAsync();
        }
        public async Task<GetUserInformationModel> GetKeyUserInformationAsync()
        {
            var user = await _inventoryDbContext.UserMst
                .FirstOrDefaultAsync(x => !string.IsNullOrEmpty(x.BusinessName));

            if (user == null)
                return null;

            return new GetUserInformationModel
            {
                BusinessName = user.BusinessName,
                BusinessAddress = user.BusinessAddress,
                Email = user.Email,
                Mobile = user.mobileNo
            };
        }
    }
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
    }
}
