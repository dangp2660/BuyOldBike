using System;

namespace BuyOldBike_DAL.Entities;

public partial class SellerProfile
{
    public Guid SellerId { get; set; }

    public double SellerRating { get; set; }

    public int TotalReviews { get; set; }

    public DateTime? LastReviewDate { get; set; }

    public virtual User Seller { get; set; } = null!;
}
