using System;
using System.Collections.Generic;
using System.Text;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

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

        public async Task<object> RegisterAsync(string? userName, string email, string password,string mobileNo=null)
        {
            await _inventoryDbContext.UserMst.AddAsync(new Domain.Entities.UserMst
            {
                Password = password,
                Email = email,
                mobileNo = mobileNo,
                UserName = userName
            });
            await _inventoryDbContext.SaveChangesAsync();
            return new AuthResponse { Success = true, Message = "Registered" };
        }
        public async Task LogoutAsync()
        {
           //await _signInManager.SignOutAsync();
        }

    }
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
    }
}
