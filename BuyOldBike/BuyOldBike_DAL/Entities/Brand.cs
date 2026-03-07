using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Brand
{
    public int BrandId { get; set; }

    public string BrandName { get; set; } = null!;

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
