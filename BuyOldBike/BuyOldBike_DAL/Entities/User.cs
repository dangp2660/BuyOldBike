using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Password { get; set; }

    public string Role { get; set; } = null!;

    // Seller reputation/feedback fields
    public double? SellerRating { get; set; } = 0;

    public int? TotalReviews { get; set; } = 0;

    public DateTime? LastReviewDate { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

    public virtual ICollection<KycProfile> KycProfiles { get; set; } = new List<KycProfile>();

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> ReviewBuyers { get; set; } = new List<Review>();

    public virtual ICollection<Review> ReviewSellers { get; set; } = new List<Review>();
}
