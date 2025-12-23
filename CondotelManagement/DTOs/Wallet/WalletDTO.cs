namespace CondotelManagement.DTOs.Wallet
{
    public class WalletDTO
    {
        public int WalletId { get; set; }
        public int? UserId { get; set; }
        public int? HostId { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsDefault { get; set; }
    }

    public class WalletCreateDTO
    {
        public int? UserId { get; set; }
        public int? HostId { get; set; }
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountHolderName { get; set; } = null!;
        public bool IsDefault { get; set; } = false;
    }

    public class WalletUpdateDTO
    {
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public string? Status { get; set; }
        public bool? IsDefault { get; set; }
    }
}

