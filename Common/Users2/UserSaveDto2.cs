namespace Common.Users2
{
    public class UserSaveDto2
    {
        /// <summary>0 for a new user.</summary>
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? MobileNo { get; set; }
        public int? RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Required for a new user. Ignored when editing (use reset password instead).</summary>
        public string? Password { get; set; }
    }
}
