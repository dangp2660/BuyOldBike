using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Payment
{
    public Guid PaymentId { get; set; }

    public Guid? UserId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
