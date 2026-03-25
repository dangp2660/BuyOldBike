using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Order
{
    public Guid OrderId { get; set; }

    public Guid? BuyerId { get; set; }

    public Guid? ListingId { get; set; }

    public string? Status { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual Listing? Listing { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
