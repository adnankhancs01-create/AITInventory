using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IUserRepo
    {
        Task<object> LoginAsync(string email, string password,bool isApiUser=true);
        Task<object> RegisterAsync(string? userName, string email, string password, string mobileNo = null);
        Task LogoutAsync();
    }
}
