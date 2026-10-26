namespace Domain.Entities
{
    /// <summary>A role is data, not an enum. The root user can add, rename and deactivate roles.</summary>
    public class UserRole2
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public ICollection<RoleScreenPermission2> Permissions { get; set; } = new List<RoleScreenPermission2>();
    }
}
