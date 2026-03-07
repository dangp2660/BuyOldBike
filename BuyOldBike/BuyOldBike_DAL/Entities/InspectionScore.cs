using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class InspectionScore
{
    public Guid InspectionId { get; set; }

    public int ComponentId { get; set; }

    public int? Score { get; set; }

    public string? Note { get; set; }

    public virtual InspectionComponent Component { get; set; } = null!;

    public virtual Inspection Inspection { get; set; } = null!;
}
