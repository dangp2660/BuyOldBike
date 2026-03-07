using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class ReturnRequest
{
    public Guid ReturnRequestId { get; set; }

    public Guid? OrderId { get; set; }

    public string? Reason { get; set; }

    public string? Detail { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual ICollection<ReturnRequestImage> ReturnRequestImages { get; set; } = new List<ReturnRequestImage>();
}
