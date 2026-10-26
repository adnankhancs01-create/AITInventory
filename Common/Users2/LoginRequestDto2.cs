namespace Common.Users2
{
    public class LoginRequestDto2
    {
        /// <summary>Email address or user name.</summary>
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
