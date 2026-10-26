namespace Common.Users2
{
    public class BusinessSetupRequestDto2
    {
        public string BusinessName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? BusinessEmail { get; set; }
        public string? TaxNumber { get; set; }

        // Owner (root) account
        public string OwnerUserName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
