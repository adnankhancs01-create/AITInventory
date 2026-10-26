namespace Common.Users2
{
    public class StartupStateDto2
    {
        /// <summary>True when no root (owner) account exists yet, so the Business Setup screen must be shown.</summary>
        public bool SetupRequired { get; set; }
    }
}
