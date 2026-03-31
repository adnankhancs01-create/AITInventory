using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Common.Models.RequestModel
{
    public class UserRequestModel
    {
    }
    public class CommonUserRequestModel
    {
        [Required(ErrorMessage = "Email required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password required")]
        public string Password { get; set; }
    }
    public class LoginModel: CommonUserRequestModel
    {

    }
    public class SignUpModel: CommonUserRequestModel
    {
        public string UserName { get; set; }
        public string? MobileNo { get; set; }

    }
}
