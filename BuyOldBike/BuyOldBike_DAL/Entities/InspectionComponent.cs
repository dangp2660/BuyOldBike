using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class InspectionComponent
{
    public int ComponentId { get; set; }

    public string? ComponentName { get; set; }

    public virtual ICollection<InspectionScore> InspectionScores { get; set; } = new List<InspectionScore>();
}
