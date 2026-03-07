using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Listing
{
    public Guid ListingId { get; set; }

    public Guid? SellerId { get; set; }

    public int? BrandId { get; set; }

    public string? Title { get; set; }

    public int? BikeTypeId { get; set; }

    public int? UsageDuration { get; set; }

    public string? FrameNumber { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ListingUrl { get; set; }

    public virtual Type? BikeType { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

    public virtual ICollection<ListingImage> ListingImages { get; set; } = new List<ListingImage>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User? Seller { get; set; }
}
