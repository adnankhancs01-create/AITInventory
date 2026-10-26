namespace Domain.Entities
{
    /// <summary>Screen registry. Synced at startup from ScreenCatalog2 so every page/menu item has a stable key.</summary>
    public class AppScreen2
    {
        public int Id { get; set; }
        public string ScreenKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        /// <summary>Administration screens that only the root user can open. They never appear in the role checklist.</summary>
        public bool IsRootOnly { get; set; }
    }
}
