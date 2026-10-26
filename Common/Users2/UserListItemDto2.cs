namespace Common.Users2
{
    public class UserListItemDto2
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsRoot { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginOn { get; set; }
    }
}
