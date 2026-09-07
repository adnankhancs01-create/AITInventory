using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IUserRepo
    {
        Task<object> LoginAsync(string email, string password,bool isApiUser=true);
        Task<object> RegisterAsync(SignUpModel signUpModel);
        Task LogoutAsync();
        Task<GetUserInformationModel> GetKeyUserInformationAsync();
    }
}
