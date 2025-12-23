using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int? UserId { get; set; }

    public int? HostId { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }

    public string? AccountHolderName { get; set; }

    public virtual Host? Host { get; set; }

    public virtual User? User { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsDefault { get; set; } = true;
}
