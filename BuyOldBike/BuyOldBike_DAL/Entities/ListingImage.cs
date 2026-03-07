using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class ListingImage
{
    public Guid ImageId { get; set; }

    public Guid? ListingId { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImageType { get; set; }

    public virtual Listing? Listing { get; set; }
}
