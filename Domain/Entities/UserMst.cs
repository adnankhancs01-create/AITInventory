using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class UserMst
    {
        [Key]
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; }
        public string? mobileNo { get; set; }
        public string? PasswordHash { get; set; }
    }
}
