using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Type
{
    public int BikeTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
