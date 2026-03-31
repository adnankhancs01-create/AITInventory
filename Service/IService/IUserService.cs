using Common;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface IUserService
    {
        Task<BaseResponse<object>> LoginAsync(string email, string password, bool isApiUser = true);
        Task<BaseResponse<object>> RegisterAsync(string userName, string email, string password, string mobileNo);
        Task LogoutAsync();
    }
}
