using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class KycImage
{
    public Guid ImageId { get; set; }

    public Guid KycId { get; set; }

    public string? ImageType { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual KycProfile Kyc { get; set; } = null!;
}
