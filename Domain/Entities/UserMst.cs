using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class UserMst
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }

        /// <summary>Legacy plain-text password. Emptied automatically at the first successful login, after PasswordHash is stored.</summary>
        public string Password { get; set; } = string.Empty;
        public string? mobileNo { get; set; }
        public string? PasswordHash { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }

        // ---------- User management (new) ----------
        public int? BusinessId { get; set; }
        public Business2? Business { get; set; }
        public int? RoleId { get; set; }
        public UserRole2? Role { get; set; }
        /// <summary>The business owner. Exactly one per database. Has access to every screen.</summary>
        public bool IsRoot { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public DateTime? LastLoginOn { get; set; }
    }
}
