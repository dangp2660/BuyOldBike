using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Inspection
{
    public Guid InspectionId { get; set; }

    public Guid ListingId { get; set; }

    public Guid? InspectorId { get; set; }

    public string? InspectionTypeId { get; set; }

    public Guid InspectionLocationId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Result { get; set; }

    public int? OverallScore { get; set; }

    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual InspectionLocation InspectionLocation { get; set; } = null!;

    public virtual ICollection<InspectionScore> InspectionScores { get; set; } = new List<InspectionScore>();

    public virtual User? Inspector { get; set; }

    public virtual Listing Listing { get; set; } = null!;
}
