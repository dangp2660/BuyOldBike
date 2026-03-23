using System;

namespace BuyOldBike_DAL.Entities;

public partial class WalletTransaction
{
    public Guid WalletTransactionId { get; set; }

    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }

    public string Direction { get; set; } = null!;

    public string Type { get; set; } = null!;

    public Guid? OrderId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual UserWallet Wallet { get; set; } = null!;
}

