using System;

namespace BuyOldBike_DAL.Entities;

public partial class WithdrawalRequest
{
    public Guid WithdrawalRequestId { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string BankName { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

