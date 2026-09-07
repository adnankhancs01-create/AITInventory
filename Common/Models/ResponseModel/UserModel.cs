using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class UserModel
    {
    }
    public class GetUserInformationModel
    {
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
    }
}
