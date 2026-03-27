using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class ReturnRequestImage
{
    public Guid ImageId { get; set; }

    public Guid ReturnRequestId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string UploaderRole { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ReturnRequest ReturnRequest { get; set; } = null!;
}
