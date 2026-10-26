namespace Common.Users2
{
    public class RoleDto2
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int UserCount { get; set; }
    }
}
