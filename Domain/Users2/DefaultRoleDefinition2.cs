namespace Domain.Users2
{
    public class DefaultRoleDefinition2
    {
        public DefaultRoleDefinition2(string name, string description, IReadOnlyList<string> screenKeys)
        {
            Name = name;
            Description = description;
            ScreenKeys = screenKeys;
        }

        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<string> ScreenKeys { get; }
    }
}
