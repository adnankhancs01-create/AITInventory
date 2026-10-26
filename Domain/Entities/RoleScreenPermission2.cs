namespace Domain.Entities
{
    /// <summary>Which screens a role may open. A missing row means no access.</summary>
    public class RoleScreenPermission2
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public UserRole2? Role { get; set; }
        public string ScreenKey { get; set; } = string.Empty;
        public bool CanView { get; set; }
    }
}
