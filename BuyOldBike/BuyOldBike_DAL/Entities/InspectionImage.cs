using System;

namespace BuyOldBike_DAL.Entities;

public partial class InspectionImage
{
    public Guid ImageId { get; set; }

    public Guid InspectionId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Inspection Inspection { get; set; } = null!;
}

