using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using Domain.IRepositories;

namespace Data.SupportiveEntities
{
    public class UserRepo : IUserRepo
    {
        private readonly UserManager<ApplicationUserIdentity> _userManager;
        private readonly SignInManager<ApplicationUserIdentity> _signInManager;

        public UserRepo(
            UserManager<ApplicationUserIdentity> userManager,
            SignInManager<ApplicationUserIdentity> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<object> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, false);

            if (!result.Succeeded)
                return new AuthResponse { Success = false, Message = "Invalid credentials" };

            return new AuthResponse { Success = true, Message = "Login success" };
        }

        public async Task<object> RegisterAsync(string userName, string email, string password)
        {
            if(string.IsNullOrEmpty(userName)) new AuthResponse
            {
                Success = false,
                Message = "user name required"
            };
            var user = new ApplicationUserIdentity
            {
                UserName = userName,
                Email = email
            };
            await _userManager.SetUserNameAsync(user, userName);
            await _userManager.SetEmailAsync(user, email);

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join(",", result.Errors.Select(e => e.Description))
                };

            return new AuthResponse { Success = true, Message = "Registered" };
        }
    }
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
    }
}
