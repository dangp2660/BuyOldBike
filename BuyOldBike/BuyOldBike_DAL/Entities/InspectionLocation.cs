using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class InspectionLocation
{
    public Guid InspectionLocationId { get; set; }

    public string Type { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string AddressLine { get; set; } = null!;

    public string? Ward { get; set; }

    public string? District { get; set; }

    public string? City { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
}
