using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class KycProfile
{
    public Guid KycId { get; set; }

    public Guid? UserId { get; set; }

    public string? IdNumber { get; set; }

    public string? FullName { get; set; }

    public string? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Nationality { get; set; }

    public string? PlaceOfOrigin { get; set; }

    public string? PlaceOfResidence { get; set; }

    public string? ExpiryDate { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public virtual ICollection<KycImage> KycImages { get; set; } = new List<KycImage>();

    public virtual User? User { get; set; }
}
