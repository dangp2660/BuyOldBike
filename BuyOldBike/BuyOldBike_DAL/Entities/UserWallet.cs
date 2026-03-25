using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class UserWallet
{
    public Guid WalletId { get; set; }

    public Guid UserId { get; set; }

    public decimal Balance { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}

