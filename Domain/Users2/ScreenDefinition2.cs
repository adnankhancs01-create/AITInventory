namespace Domain.Users2
{
    public class ScreenDefinition2
    {
        public ScreenDefinition2(string key, string name, string group, string route, int order, bool rootOnly = false)
        {
            ScreenKey = key;
            DisplayName = name;
            GroupName = group;
            Route = route;
            SortOrder = order;
            IsRootOnly = rootOnly;
        }

        public string ScreenKey { get; }
        public string DisplayName { get; }
        public string GroupName { get; }
        /// <summary>Route prefix, for example "/stock2/overview". Sub-paths such as "/retail/5" belong to the same screen.</summary>
        public string Route { get; }
        public int SortOrder { get; }
        public bool IsRootOnly { get; }
    }
}
