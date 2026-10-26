namespace Common.Users2
{
    /// <summary>Who is logged in and which screens they may open. Built at login and kept by CurrentUser2.</summary>
    public class SessionDto2
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsRoot { get; set; }
        public int? BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public List<string> AllowedScreens { get; set; } = new();
    }
}
