using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Review
{
    public Guid ReviewId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? BuyerId { get; set; }

    public Guid? SellerId { get; set; }

    public int? Rating { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? Seller { get; set; }
}
